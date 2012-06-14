using NServiceBus;
using NServiceBus.Config;
using NServiceBus.Unicast.Transport.Transactional.Config;

namespace MyClient
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Client, IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                .SupressDTC()
                .InMemorySubscriptionStorage()
                .UseInMemoryTimeoutPersister()
                .DisableSecondLevelRetries()
                .DisableNotifications()
                .InMemoryFaultManagement()
                .MsmqTransport()
                    .IsTransactional(false);
        }
    }
}