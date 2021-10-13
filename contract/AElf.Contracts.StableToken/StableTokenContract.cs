using System.Linq;
using AElf.Contracts.TokenHolder;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.StableToken
{
    public partial class StableTokenContract : StableTokenContractContainer.StableTokenContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenHolderContract.Value == null, "Already initialized.");
            State.TokenHolderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.RandomNumberProviderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            State.StableTokenSymbolList.Value = input.InitialStableTokenSymbolList;
            var createSchemeInput = new CreateTokenHolderProfitSchemeInput
            {
                Symbol = ShareTokenSymbol,
            };
            State.TokenHolderContract.CreateScheme.Send(createSchemeInput);
            return new Empty();
        }

        public override Empty TransferCallback(TransferInput input)
        {
            // Record share token distribution.
            var shareTokenAmount = CalculateShareTokenAmount(input.Amount);
            
            
            return new Empty();
        }

        private long CalculateShareTokenAmount(long transferAmount)
        {
            // TODO
            return transferAmount;
        }
    }
}