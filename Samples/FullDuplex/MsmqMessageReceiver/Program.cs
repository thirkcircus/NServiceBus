using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MsmqMessageReceiver
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if ((args.Length != 5))
            {
                Console.Write(
                    "Usage: MsmqMessageReceiver [queue-name] [false/true for trx queues] [number-of-receiving-threads] [messages-to-read] [peek/receive: peek for peek and receive]");
                Environment.Exit(0);
            }
            
            int numberOfThreads;
            int.TryParse(args[2], out numberOfThreads);

            var threads = new List<Thread>();
            for (int i = 0; i < numberOfThreads; i++)
            {
                var receiver = new Receiver(args[0], Convert.ToBoolean(args[1]), int.Parse(args[3]), args[4]);
                var t = new Thread(receiver.Receive);
                threads.Add(t);
                t.Start(); //start the new thread
            }
            Stopwatch watch = Stopwatch.StartNew();
            foreach (Thread t in threads)
                t.Join();

            watch.Stop();
            Console.WriteLine("After waiting for [{0}] threads. Time to receive messages is: [{1}]", numberOfThreads,
                              watch.Elapsed.TotalSeconds);
        }
    }
}