using NServiceBus;

namespace MyClient
{
    public class PreventSubscription : IWantCustomInitialization
    {
        public void Init()
        {
            //so we don't end up subscribing to DataResponseMessage
            Configure.Instance.UnicastBus().DoNotAutoSubscribe();
        }
    }
}
