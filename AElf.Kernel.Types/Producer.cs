using AElf.Common;

namespace AElf.Kernel.Types
{
    public sealed class Producer
    {
        public byte[] PubKey { get; set; }
        public ulong TicketCount { get; set; }
        public int Order { get; set; }
    }
}