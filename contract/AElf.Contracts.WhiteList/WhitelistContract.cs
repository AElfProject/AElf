using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Whitelist
{
    public partial class WhitelistContract : WhitelistContractContainer.WhitelistContractBase
    {
        public override Empty Initialize(Empty input)
        {
            return new Empty();
        }
    }
}