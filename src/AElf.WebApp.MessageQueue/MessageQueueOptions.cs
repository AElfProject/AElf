namespace AElf.WebApp.MessageQueue
{
    public class MessageQueueOptions
    {
        public bool Enable { get; set; } = true;
        public string HostName { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5672;
        public string ClientName { get; set; } = "AElf";
        public string ExchangeName { get; set; } = "AElfExchange";
    }
}