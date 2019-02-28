using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Resource
{
    public class ResourceIssued : Event<ResourceIssued>
    {
        [Indexed] public string ResourceType { get; set; }
        public ulong IssuedAmount { get; set; }
    }

    public class ResourceBought : Event<ResourceBought>
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address Buyer { get; set; }
        public ulong PaidElf { get; set; }
        public ulong ReceivedResource { get; set; }
    }

    public class ResourceSold : Event<ResourceSold>
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address Seller { get; set; }
        public ulong PaidResource { get; set; }
        public ulong ReceivedElf { get; set; }
    }

    public class ResourceLocked : Event<ResourceLocked>
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address User { get; set; }
        public ulong Amount { get; set; }
    }

    public class ResourceUnlocked : Event<ResourceUnlocked>
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address User { get; set; }
        public ulong Amount { get; set; }
    }
}