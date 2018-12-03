using AElf.Common;

namespace AElf.Kernel.Types
{
    public sealed class Node
    {
        public Address Address { get; set; }
        public ulong TicketCount { get; set; }
        public int Order { get; set; }
    }
}