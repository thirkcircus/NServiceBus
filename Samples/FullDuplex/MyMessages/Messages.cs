using System;
using NServiceBus;

namespace MyMessages
{
    [Express]
    [Serializable]
    public class RequestDataMessage : ICommand
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
    }
}