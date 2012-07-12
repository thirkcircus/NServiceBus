using System;
using System.Diagnostics;
using System.Threading;

namespace MsmqMessageReceiver
{
    public class Receiver
    {
        private static int i;
        private readonly int messagesToRead;
        private readonly string methodToExecute;
        private readonly string receivingQueueName;
        private readonly bool transactionalQueue;

        public Receiver(string queueName, bool trx, int messages, string method)
        {
            transactionalQueue = trx;
            receivingQueueName = queueName;
            messagesToRead = messages;
            methodToExecute = method;
        }

        public void Receive()
        {
            int messageCount = 0;
            var recv = new MsmqMessageReceiver();
            recv.TransactionalQueue = transactionalQueue;
            recv.Init(receivingQueueName, transactionalQueue);
            Stopwatch sw = Stopwatch.StartNew();
            if (methodToExecute.StartsWith("Peek"))
                messageCount = PeekAndReceive(messageCount, recv);
            else
                messageCount = JustReceive(messageCount, recv);
            sw.Stop();

            Console.WriteLine("Thread: [{0}] Received: [{1}] messages in: [{2}] seconds",
                              Thread.CurrentThread.ManagedThreadId, messageCount, sw.Elapsed.TotalSeconds);
        }

        private int PeekAndReceive(int messageCount, MsmqMessageReceiver recv)
        {
            while (i < messagesToRead)
            {
                recv.HasMessage();
                recv.Receive();
                messageCount++;
                Interlocked.Increment(ref i);
            }
            return messageCount;
        }

        private int JustReceive(int messageCount, MsmqMessageReceiver recv)
        {
            while (i < messagesToRead)
            {
                recv.Receive();
                messageCount++;
                Interlocked.Increment(ref i);
            }
            return messageCount;
        }
    }
}