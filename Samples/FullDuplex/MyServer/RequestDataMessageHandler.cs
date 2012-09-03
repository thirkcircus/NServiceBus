using System;
using System.Diagnostics;
using System.Threading;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        private const int Max = 1000;
        private static bool first = true;
        private static readonly Stopwatch sw = new Stopwatch();
        private static int counter;
        private static int counter2;
        public IBus Bus { get; set; }

        #region IHandleMessages<RequestDataMessage> Members

        public void Handle(RequestDataMessage message)
        {
            if (first)
            {
                lock (typeof(int))
                {
                    sw.Start();
                    first = false;
                }
            }
            Interlocked.Increment(ref counter);
            Interlocked.Increment(ref counter2);
            lock (typeof(int))
            {
                if (counter <= Max) 
                    return;
                Console.WriteLine("Thread: [{0}] Time to receive: [{1}] messages is: [{2}], message counter is: [{3}]", Thread.CurrentThread.ManagedThreadId, counter, sw.Elapsed.TotalSeconds, counter2);
                Interlocked.Exchange(ref counter, 0); 
                sw.Restart();
            }
        }

        #endregion
    }
}