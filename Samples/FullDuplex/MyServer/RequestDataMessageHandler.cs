using System;
using System.Diagnostics;
using System.Threading;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        const int Max = 1000;
        static bool first = true;
        static readonly Stopwatch sw = new Stopwatch();
        static int counter;
        static int messageCount;
        public IBus Bus { get; set; }
        static readonly object myLock = new object();

        #region IHandleMessages<RequestDataMessage> Members
        public RequestDataMessageHandler()
        {
            //Interlocked.Increment(ref messageCount);
            //Console.WriteLine("Thread: [{0}] Time to receive: [{1}] messages is: [{2}], message counter is: [{3}]", 
            //    Thread.CurrentThread.ManagedThreadId, 
            //    counter, 
            //    sw.Elapsed.TotalSeconds, 
            //    messageCount);
        }
        public void Handle(RequestDataMessage message)
        {
            if (first)
            {
                lock (myLock)
                {
                    sw.Start();
                    first = false;
                }
            }
            Interlocked.Increment(ref messageCount);
            Interlocked.Increment(ref counter);
            
            lock (myLock)
            {
                if (counter < Max) 
                    return;
                Console.WriteLine("Thread: [{0}] Time to receive: [{1}] messages is: [{2}], message counter is: [{3}]", Thread.CurrentThread.ManagedThreadId, counter, sw.Elapsed.TotalSeconds, messageCount);
                Interlocked.Exchange(ref counter, 0); 
                sw.Restart();
            }
        }

        #endregion
    }
}