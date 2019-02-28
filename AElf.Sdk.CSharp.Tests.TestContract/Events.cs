using AElf.Common;

namespace AElf.Sdk.CSharp.Tests.TestContract
{
    public class Transferred : Event<Transferred>
    {
        [Indexed] public Address From { get; set; }
        [Indexed] public Address To { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Approved : Event<Approved>
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }


    public class UnApproved : Event<UnApproved>
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Burned : Event<Burned>
    {
        public Address Burner { get; set; }
        public ulong Amount { get; set; }
    }
}