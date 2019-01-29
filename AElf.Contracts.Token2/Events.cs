using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Token
{
    public class Transferred : Event
    {
        [Indexed] public Address From { get; set; }
        [Indexed] public Address To { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Approved : Event
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }


    public class UnApproved : Event
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Burned : Event
    {
        public Address Burner { get; set; }
        public ulong Amount { get; set; }
    }
}