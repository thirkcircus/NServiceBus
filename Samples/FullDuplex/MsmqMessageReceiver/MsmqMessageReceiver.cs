using System;
using System.Diagnostics;
using System.Messaging;
using System.Net;
using System.Security.Principal;
using NServiceBus;

namespace MsmqMessageReceiver
{
    public class MsmqMessageReceiver
    {
        public void Init(string address, bool transactional)
        {
            Init(Address.Parse(address), transactional);
        }

        public void Init(Address address, bool transactional)
        {
            useTransactions = transactional;

            if (address == null)
                throw new ArgumentException("Input queue must be specified");

            var machine = address.Machine;

            if (machine.ToLower() != Environment.MachineName.ToLower())
                throw new InvalidOperationException(string.Format("Input queue [{0}] must be on the same machine as this process [{1}].",
                    address, Environment.MachineName.ToLower()));

            myQueue = new MessageQueue(GetFullPath(address));

            if (useTransactions && !QueueIsTransactional())
                throw new ArgumentException("Queue must be transactional (" + address + ").");

            var mpf = new MessagePropertyFilter();
            mpf.SetAll();

            myQueue.MessageReadPropertyFilter = mpf;

            if (PurgeOnStartup)
                myQueue.Purge();
        }

        public bool HasMessage()
        {
            try
            {
                var m = myQueue.Peek(TimeSpan.FromMilliseconds(1));
                return true;
            }
            catch (MessageQueueException e)
            {
                if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    // No message was in the queue.
                    return false;
                }
            }
            return true;
        }

        public Message Receive(bool trx)
        {
            try
            {
                Message m;
                if(trx == false)
                    m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), MessageQueueTransactionType.None);
                else
                    m = myQueue.Receive(TimeSpan.FromSeconds(secondsToWait), GetTransactionTypeForReceive());

                return m;
            }
            catch (MessageQueueException mqe)
            {
                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    return null;

                if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.AccessDenied)
                {
                    string errorException = string.Format("Do not have permission to access queue [{0}]. Make sure that the current user [{1}] has permission to Send, Receive, and Peek  from this queue.", myQueue.QueueName, WindowsIdentity.GetCurrent() != null ? WindowsIdentity.GetCurrent().Name : "unknown user");
                    Console.WriteLine(errorException);
                    throw new InvalidOperationException(errorException, mqe);
                }

                throw;
            }
        }

        bool QueueIsTransactional()
        {
            try
            {
                return myQueue.Transactional;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("There is a problem with the input queue: {0}. See the enclosed exception for details.", myQueue.Path), ex);
            }
        }

        private MessageQueueTransactionType GetTransactionTypeForReceive()
        {
            return useTransactions ? MessageQueueTransactionType.Automatic : MessageQueueTransactionType.None;
        }

        public static string GetFullPath(Address value)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(value.Machine, out ipAddress))
                return PREFIX_TCP + GetFullPathWithoutPrefix(value);

            return PREFIX + GetFullPathWithoutPrefix(value);
        }
        public static string GetFullPathWithoutPrefix(string value)
        {
            return getMachineNameFromLogicalName(value) + PRIVATE + getQueueNameFromLogicalName(value);
        }
        public static string GetFullPathWithoutPrefix(Address address)
        {
            return address.Machine + PRIVATE + address.Queue;
        }
        private static string getMachineNameFromLogicalName(string logicalName)
        {
            string[] arr = logicalName.Split('@');

            string machine = Environment.MachineName;

            if (arr.Length >= 2)
                if (arr[1] != "." && arr[1].ToLower() != "localhost")
                    machine = arr[1];

            return machine;
        }
        private static string getQueueNameFromLogicalName(string logicalName)
        {
            string[] arr = logicalName.Split('@');

            if (arr.Length >= 1)
                return arr[0];

            return null;
        }

        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        public bool PurgeOnStartup { get; set; }


        private int secondsToWait = 1;
        public int SecondsToWaitForMessage
        {
            get { return secondsToWait; }
            set { secondsToWait = value; }
        }

        private MessageQueue myQueue;

        private bool useTransactions;
        private const string DIRECTPREFIX = "DIRECT=OS:";
        private static readonly string DIRECTPREFIX_TCP = "DIRECT=TCP:";
        private readonly static string PREFIX_TCP = "FormatName:" + DIRECTPREFIX_TCP;
        private static readonly string PREFIX = "FormatName:" + DIRECTPREFIX;
        private const string PRIVATE = "\\private$\\";

    }
}