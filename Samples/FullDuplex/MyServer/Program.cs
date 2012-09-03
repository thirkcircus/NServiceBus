using System;
using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Unicast.Transport.Transactional.Config;

namespace MyServer
{
    internal class Program
    {
        private static void Main()
        {
            Configure.With()
                .DefaultBuilder()
                .Log4Net()
                .XmlSerializer()
                //.BinarySerializer()
                //.MsmqTransport()
                .SqlServerTransport(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=SiteB;Integrated Security=True;")
                .PurgeOnStartup(false)
                //.DoNotCreateQueues()
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