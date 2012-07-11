using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MsmqMessageReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            int numberOfThreads;
            int.TryParse(args[2], out numberOfThreads);
            if ((numberOfThreads == 0) || (args.Length != 5))
            {
                Console.Write("Usage: MsmqMessageReceiver [queue-name] [false/true for trx queues] [number-of-receiving-threads] [messages-to-read] [peek/receive: peek for peek and receive]");
                Environment.Exit(0);
            }
            
            var threads = new List<Thread>();
            for (var i = 0; i < numberOfThreads; i++)
            {
                var receiver = new Receiver(args[0], Convert.ToBoolean(args[1]), int.Parse(args[3]), args[4]);
                var t = new Thread(receiver.Receive);
                threads.Add(t);
                t.Start();//start the new thread
            }
            var watch = Stopwatch.StartNew();
            foreach (var t in threads)
                t.Join();
            
            watch.Stop();
            Console.WriteLine("After waiting for [{0}] threads. Time to receive messages is: [{1}]", numberOfThreads, watch.Elapsed.TotalSeconds);
        }
    }
}
