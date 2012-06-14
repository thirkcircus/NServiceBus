using System;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Logging;
using NServiceBus.Unicast.Transport.Transactional.Config;

namespace MyServer
{
    class Program
    {
        static void Main()
        {
            Configure.With()
                .DefaultBuilder()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(false)
                    .PurgeOnStartup(false)
                    .DoNotCreateQueues()
                    .SupressDTC()
                .DisableSecondLevelRetries()
                .DisableTimeoutManager()
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
