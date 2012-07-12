using System;
using System.Diagnostics;
using System.Threading;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        private const int Max = 10000;
        private static bool first = true;
        private static readonly Stopwatch sw = new Stopwatch();
        private static int counter;
        public IBus Bus { get; set; }

        #region IHandleMessages<RequestDataMessage> Members

        public void Handle(RequestDataMessage message)
        {
            if (first)
            {
                sw.Start();
                first = false;
            }
            Interlocked.Increment(ref counter);
            if ((counter >= Max))
            {
                Console.WriteLine(string.Format("Thread: [{0}] Time to receive: [{1}] messages is: [{2}]",
                                                Thread.CurrentThread.ManagedThreadId, counter, sw.Elapsed.TotalSeconds));
                Interlocked.Exchange(ref counter, 0);
                sw.Restart();
            }
        }

        #endregion
    }
}