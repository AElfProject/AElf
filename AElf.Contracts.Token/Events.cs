using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Token
{
    public class Transferred : Event<Transferred>
    {
        [Indexed] public Address From { get; set; }
        [Indexed] public Address To { get; set; }
        public ulong Amount { get; set; }
    }

    public class Approved : Event<Approved>
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        public ulong Amount { get; set; }
    }


    public class UnApproved : Event<UnApproved>
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        public ulong Amount { get; set; }
    }

    public class Burned : Event<Burned>
    {
        [Indexed] public Address Burner { get; set; }
        public ulong Amount { get; set; }
    }
}