using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMinter
{
    public partial class NFTMinterContract : NFTMinterContractContainer.NFTMinterContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.NFTContract.Value = input.NftContractAddress;
            State.AdminAddress.Value = input.AdminAddress ?? Context.Sender;
            State.RandomNumberProviderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }
    }
}