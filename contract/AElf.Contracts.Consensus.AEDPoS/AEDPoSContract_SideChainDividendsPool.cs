using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.CSharp.Core;
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

            var currentReceivedDividends = State.SideChainReceivedDividends[Context.CurrentHeight];
            if (currentReceivedDividends.Value.ContainsKey(input.Symbol))
            {
                currentReceivedDividends.Value[input.Symbol] =
                    currentReceivedDividends.Value[input.Symbol].Add(input.Amount);
            }
            else
            {
                currentReceivedDividends.Value.Add(input.Symbol, input.Amount);
            }

            State.SideChainReceivedDividends[Context.CurrentHeight] = currentReceivedDividends;

            Context.LogDebug(() => $"Contributed {input.Amount} {input.Symbol}s to side chain dividends pool.");

            return new Empty();
        }

        public override SideChainDividends GetSideChainDividends(Int64Value input)
        {
            Assert(Context.CurrentHeight > input.Value, "Cannot query dividends of a future block.");
            return State.SideChainReceivedDividends[input.Value];
        }

        private void ReleaseSideChainDividendsPool()
        {
            if (State.TokenHolderContract.Value == null) return;
            var scheme = State.TokenHolderContract.GetScheme.Call(Context.Self);
            var isTimeToRelease =
                (Context.CurrentBlockTime - State.BlockchainStartTimestamp.Value).Seconds
                .Div(State.PeriodSeconds.Value) > scheme.Period - 1;
            Context.LogDebug(() => "ReleaseSideChainDividendsPool Information:\n" +
                                   $"CurrentBlockTime: {Context.CurrentBlockTime}\n" +
                                   $"BlockChainStartTime: {State.BlockchainStartTimestamp.Value}\n" +
                                   $"PeriodSeconds: {State.PeriodSeconds.Value}\n" +
                                   $"Scheme Period: {scheme.Period}");
            if (isTimeToRelease)
            {
                Context.LogDebug(() => "Ready to release side chain dividends pool.");
                State.TokenHolderContract.DistributeProfits.Send(new DistributeProfitsInput
                {
                    SchemeManager = Context.Self
                });
            }
        }
    }
}