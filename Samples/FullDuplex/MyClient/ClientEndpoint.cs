using System;
using System.Diagnostics;
using System.Globalization;
using MyMessages;
using NServiceBus;

namespace MyClient
{
    public class ClientEndpoint : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }
        private const int Max = 40000;
        public void Run()
        {
            Console.WriteLine("Sending messages: " + Max);
            var counter = 0;
            var requestDataMessage = new RequestDataMessage[Max];

            while (counter < Max)
            {
                requestDataMessage[counter] = new RequestDataMessage()
                {
                    DataId = new Guid(),
                    String = counter.ToString(CultureInfo.InvariantCulture)
                };
                counter++;
            }
            
            counter = 0;
            var watch = new Stopwatch();
            watch.Start();
            while (counter < Max)
            {
                Bus.Send(requestDataMessage[counter]);
                counter++;
                if (counter % 10000 == 0)
                {
                    Console.WriteLine("Time to send: [{0}] messages is: [{1}]", counter, watch.Elapsed.TotalMilliseconds);
                    watch.Restart();
                }
            }
            
            watch.Stop();
        }

        /// <summary>
        /// Method called at startup.
        /// </summary>
        public void Start()
        {
            Run();
        }

        public void Stop()
        {
            Console.WriteLine("SToppppp was called");
        }
    }
}
