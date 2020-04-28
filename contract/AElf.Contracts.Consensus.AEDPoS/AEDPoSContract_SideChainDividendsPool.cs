using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public override Empty ContributeToSideChainDividendsPool(ContributeToSideChainDividendsPoolInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount,
                To = Context.Self
            });

            State.TokenContract.Approve.Send(new ApproveInput
            {
                Symbol = input.Symbol,
                Amount = input.Amount,
                Spender = State.TokenHolderContract.Value
            });

            State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
            {
                SchemeManager = Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount
            });

            Context.Fire(new SideChainDonationReceived
            {
                From = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount
            });

            Context.LogDebug(() => $"Contributed {input.Amount} {input.Symbol}s to side chain dividends pool.");

            return new Empty();
        }
    }
}