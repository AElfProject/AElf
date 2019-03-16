using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Resource
{
    public class ResourceIssued : Event
    {
        [Indexed] public string ResourceType { get; set; }
        public long IssuedAmount { get; set; }
    }

    public class ResourceBought : Event
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address Buyer { get; set; }
        public long PaidElf { get; set; }
        public long ReceivedResource { get; set; }
    }

    public class ResourceSold : Event
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address Seller { get; set; }
        public long PaidResource { get; set; }
        public long ReceivedElf { get; set; }
    }

    public class ResourceLocked : Event
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address User { get; set; }
        public long Amount { get; set; }
    }

    public class ResourceUnlocked : Event
    {
        [Indexed] public string ResourceType { get; set; }
        [Indexed] public Address User { get; set; }
        public long Amount { get; set; }
    }
}