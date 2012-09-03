using System;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Unicast.Transport.Transactional.Config;

namespace MyClient
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: MyClient [number of threads] [number of messages to send]");
                return;
            }

            ClientEndpoint.NumberOfThreads = int.Parse(args[0]);
            ClientEndpoint.MessagesToSend = int.Parse(args[1]);
            Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                //.BinarySerializer()
                //.MsmqTransport()
                .SqlServerTransport(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=SiteB;Integrated Security=True;Pooling=False")
                //.IsTransactional(false)
                .PurgeOnStartup(false)
                .DoNotCreateQueues()
                //.SuppressDTC()
                .DisableSecondLevelRetries()
                .DisableNotifications()
                .InMemoryFaultManagement()
                .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers()
                .CreateBus()
                .Start();
            //.Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());

            Console.Read();
        }
    }
}