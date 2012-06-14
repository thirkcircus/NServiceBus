using System;
using System.Diagnostics;
using MyMessages;
using NServiceBus;

namespace MyServer
{
    public class RequestDataMessageHandler : IHandleMessages<RequestDataMessage>
    {
        public IBus Bus { get; set; }
        private static bool first = true;
        private static Stopwatch sw = new Stopwatch();
        private static int counter;
        private const int Max = 10000;
        
        public void Handle(RequestDataMessage message)
        {
            if (first)
            {
                sw.Start();
                first = false;
            }
            counter++;
            if ((counter % Max) == 0)
            {
                Console.WriteLine(string.Format("Time to receive: [{0}] messages is: [{1}]", counter, sw.Elapsed.TotalMilliseconds));
                sw.Restart();
            }
        }
    }
}
