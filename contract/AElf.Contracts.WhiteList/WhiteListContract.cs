using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.WhiteList
{
    public partial class WhiteListContract : WhiteListContractContainer.WhiteListContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            return new Empty();
        }
    }
}