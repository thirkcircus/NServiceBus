using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
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

        public static int NumberOfThreads = 1;
        public static int MessagesToSend = 1;
        /// <summary>
        /// Method called at startup.
        /// </summary>
        public void Start()
        {
            var threads = new List<Thread>();
            for (var i = 0; i < NumberOfThreads; i++)
            {
                
                var messageSender = new MessageSender(MessagesToSend, Bus);

                var t = new Thread(new ThreadStart(messageSender.SendMessages));
                threads.Add(t);
                t.Start();//start the new thread
            }
            var watch = new Stopwatch();
            watch.Start(); 
            foreach (var t in threads)
                t.Join();
            watch.Stop();
            Console.WriteLine("After waiting for [{0}] threads. Time to send: [{1}] messages is: [{2}]", NumberOfThreads, NumberOfThreads * MessagesToSend, watch.Elapsed.TotalSeconds);
            Stop();
        }

        public void Stop()
        {
            Console.WriteLine("SToppppp was called");
        }
    }
}
