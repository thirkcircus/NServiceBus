using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;


namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureServiceBusTopicPublisher : IPublishMessages
    {
        public const int DefaultBackoffTimeInSeconds = 10;
        public int MaxDeliveryCount { get; set; }

        public MessagingFactory Factory { get; set; }
        public NamespaceManager NamespaceClient { get; set; }

        private readonly Dictionary<string, TopicClient> senders = new Dictionary<string, TopicClient>();
        private static readonly object SenderLock = new Object();

        public bool Publish(TransportMessage message, IEnumerable<Type> eventTypes)
        {
            var topic = Address.Local.Queue + ".events"; // how?

            var sender = GetTopicClientForDestination(topic);

            if (sender == null) return false;

            if (Transaction.Current == null)
                Send(message, sender);
            else
                Transaction.Current.EnlistVolatile(new SendResourceManager(() => Send(message, sender)), EnlistmentOptions.None);

            return true;
        }

        // todo, factor out... to bad IMessageSender is internal
        private void Send(TransportMessage message, TopicClient sender)
        {
            var numRetries = 0;
            var sent = false;

            while (!sent)
            {
                try
                {
                    SendTo(message, sender);
                    sent = true;
                }
                // todo, outbox
                catch (MessagingEntityDisabledException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
                // back off when we're being throttled
                catch (ServerBusyException)
                {
                    numRetries++;

                    if (numRetries >= MaxDeliveryCount) throw;

                    Thread.Sleep(TimeSpan.FromSeconds(numRetries * DefaultBackoffTimeInSeconds));
                }
            }
        }

        // todo, factor out... to bad IMessageSender is internal
        private void SendTo(TransportMessage message, TopicClient sender)
        {
            using (var brokeredMessage = message.Body != null ? new BrokeredMessage(message.Body) : new BrokeredMessage())
            {
                if (!string.IsNullOrWhiteSpace(message.CorrelationId)) brokeredMessage.CorrelationId = message.CorrelationId;
                if (message.TimeToBeReceived < TimeSpan.MaxValue) brokeredMessage.TimeToLive = message.TimeToBeReceived;

                foreach (var header in message.Headers)
                {
                    brokeredMessage.Properties[header.Key] = header.Value;
                }

                brokeredMessage.Properties["MessageIntent"] = message.MessageIntent.ToString();
                brokeredMessage.MessageId = message.Id;
                brokeredMessage.ReplyTo = message.ReplyToAddress.ToString();

                sender.Send(brokeredMessage);
            }
        }

        // todo, factor out...
        private TopicClient GetTopicClientForDestination(string destination)
        {
            TopicClient sender;
            if (!senders.TryGetValue(destination, out sender))
            {
                lock (SenderLock)
                {
                    if (!senders.TryGetValue(destination, out sender))
                    {
                        try
                        {
                            sender = Factory.CreateTopicClient(destination);
                            senders[destination] = sender;
                        }
                        catch (MessagingEntityNotFoundException)
                        {
                            // TopicNotFoundException?
                            //throw new QueueNotFoundException { Queue = Address.Parse(destination) };
                        }
                    }
                }
            }
            return sender;
        }
    }
}