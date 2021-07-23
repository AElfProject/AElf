using System.Collections.Generic;

namespace AElf.WebApp.MessageQueue
{
    public class MessageQueueOptions
    {
        public bool Enable { get; set; } = true;
        public string HostName { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5672;
        public string ClientName { get; set; } = "AElf";
        public string ExchangeName { get; set; } = "AElfExchange";
        public string UserName { get; set; } = "aelf";
        public string PassWord { get; set; } = "12345678";
        public long StartPublishMessageHeight { get; set; }
        public int PublishStep { get; set; } = 20;
        public MessageFilter MessageFilter { get; set; }
    }

    public class MessageFilter
    {
        public MessageFilterMode Mode { get; set; }
        public List<string> ToAddresses { get; set; }
        public List<string> FromAddresses { get; set; }
        
        public List<AddressEventName> EventNamesWithAddress { get; set; }
    }

    public class AddressEventName
    {
        public string EventAddress { get; set; }
        public List<string> EventNames { get; set; } 
    }
}