using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.MultiToken
{
    public class Transferred : Event
    {
        [Indexed] public Address From { get; set; }
        [Indexed] public Address To { get; set; }
        [Indexed] public string Symbol { get; set; }
        public long Amount { get; set; }
        public string Memo { get; set; }
    }

    public class Approved : Event
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public string Symbol { get; set; }
        public long Amount { get; set; }
    }


    public class UnApproved : Event
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public string Symbol { get; set; }
        public long Amount { get; set; }
    }

    public class Burned : Event
    {
        [Indexed] public Address Burner { get; set; }
        [Indexed] public string Symbol { get; set; }
        public long Amount { get; set; }
    }
}