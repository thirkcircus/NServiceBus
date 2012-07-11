using System;
using System.Diagnostics;
using System.Threading;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        public IBus Bus { get; set; }
        private static bool first = true;
        private static Stopwatch sw = new Stopwatch();
        private static int counter = 0;
        private const int Max = 10000;
        
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
                Interlocked.Exchange(ref counter, 0);
                Console.WriteLine(string.Format("Thread: [{0}] Time to receive: [{1}] messages is: [{2}]",
                                                Thread.CurrentThread.ManagedThreadId, counter, sw.Elapsed.TotalSeconds));
                sw.Restart();
            }
        }
    }
}
