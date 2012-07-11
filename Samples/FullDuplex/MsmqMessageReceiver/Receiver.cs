using System;
using System.Diagnostics;
using System.Threading;

namespace MsmqMessageReceiver
{
    public class Receiver
    {
        private bool transactionalQueue;
        private string receivingQueueName;
        private int messagesToRead;
        private string methodToExecute;
        public Receiver(string queueName, bool trx, int messages, string method)
        {
            transactionalQueue = trx;
            receivingQueueName = queueName;
            messagesToRead = messages;
            methodToExecute = method;
        }

        public void Receive()
        {
            var messageCount = 0;
            var recv = new MsmqMessageReceiver();
            recv.Init(receivingQueueName, transactionalQueue);
            var sw = Stopwatch.StartNew();
            if (methodToExecute.StartsWith("Peek"))
                messageCount = PeekAndReceive(messageCount, recv);
            else
                messageCount = JustReceive(messageCount, recv);
            sw.Stop();

            Console.WriteLine("Thread: [{0}] Received: [{1}] messages in: [{2}] seconds", Thread.CurrentThread.ManagedThreadId, messageCount, sw.Elapsed.TotalSeconds);
        }

        private static int i = 0;
        
        private int PeekAndReceive(int messageCount, MsmqMessageReceiver recv)
        {
            while (i < messagesToRead)
            {
                recv.HasMessage();
                recv.Receive(transactionalQueue);
                messageCount++;
                Interlocked.Increment(ref i);
            }
            return messageCount;
        }
        private int JustReceive(int messageCount, MsmqMessageReceiver recv)
        {
            while (i < messagesToRead)
            {
                recv.Receive(transactionalQueue);
                messageCount++;
                Interlocked.Increment(ref i);
            }
            return messageCount;
        }

    }
}
