using System;
using System.Threading;
using Events;
using NServiceBus;

namespace Endpoints.EndpointA
{
    public class ServerEndpoint : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
        {
            Console.WriteLine("Press 'Enter' to publish a message.");

            while (true)
            {
                Bus.Publish<EventOccurred>();
                Thread.Sleep(3000);
                Console.WriteLine("Published event.");
            }
        }

        public void Stop()
        {

        }
    }
}