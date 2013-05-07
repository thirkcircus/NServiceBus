﻿namespace NServiceBus.Features
{
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions.SubcriberSideFiltering;

    public class MessageDrivenSubscriptions : IFeature
    {
        public void Initialize()
        {
            Configure.Component<MessageDrivenSubscriptionManager>(DependencyLifecycle.SingleInstance);
            Configure.Component<FilteringMutator>(DependencyLifecycle.InstancePerCall);
            Configure.Component<SubscriptionPredicatesEvaluator>(DependencyLifecycle.SingleInstance);
        }
    }
}