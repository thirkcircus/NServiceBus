using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using MyMessages;
using NServiceBus;

namespace MyClient
{
    using System.Threading.Tasks;

    public class ClientEndpoint : IWantToRunWhenBusStartsAndStops
    {
        private const int Max = 40000;

        public static int NumberOfThreads = 1;
        public static int MessagesToSend = 1;
        public IBus Bus { get; set; }

        #region IWantToRunWhenBusStartsAndStops Members

        /// <summary>
        /// Method called at startup.
        /// </summary>
        public void Start()
        {
            int counter = 0;

            //Parallel.For(0, MessagesToSend, new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads }, i =>
            //    {
            //        var requestMessage = new RequestDataMessage { DataId = new Guid(), String = (++counter).ToString(CultureInfo.InvariantCulture) };
            //        Bus.Send(requestMessage);
            //        });
            //watch.Stop();
            //Console.WriteLine("After waiting for [{0}] threads. Time to send: [{1}] messages is: [{2}]", NumberOfThreads,
            //                  NumberOfThreads * MessagesToSend, watch.Elapsed.TotalSeconds);
            var threads = new List<Thread>();
            for (int i = 0; i < NumberOfThreads; i++)
            {
                var messageSender = new MessageSender(MessagesToSend, Bus);
                var t = new Thread(messageSender.SendMessages);
                threads.Add(t);
                t.Start(); //start the new thread
            }
            var watch = new Stopwatch();
            watch.Start();
            foreach (Thread t in threads)
                t.Join();
            watch.Stop();
            Console.WriteLine("After waiting for [{0}] threads. Time to send: [{1}] messages is: [{2}]", NumberOfThreads,
                              NumberOfThreads * MessagesToSend, watch.Elapsed.TotalSeconds);
            Stop();
        }

        public void Stop()
        {
            Console.WriteLine("SToppppp was called");
            Environment.Exit(0);
        }

        #endregion
   
    }
}