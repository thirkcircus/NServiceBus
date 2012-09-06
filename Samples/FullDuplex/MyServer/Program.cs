using System;
using NServiceBus;
using NServiceBus.Config;

namespace MyServer
{
    using NServiceBus.Unicast.Transport.Transactional.Config;

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
                //sTransacional(true)
                .PurgeOnStartup(false)
                //.DoNotCreateQueues()
                .SuppressDTC()
                .DisableSecondLevelRetries()
                .DisableNotifications()
                .InMemoryFaultManagement()
                .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers()
                .CreateBus()
                //.Start();
            .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());


            Console.Read();
        }
    }
}