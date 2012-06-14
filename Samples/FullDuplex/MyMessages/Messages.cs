using NServiceBus;
using System;

namespace MyMessages
{
//    [Express]
    public class RequestDataMessage : ICommand
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
    }
}
