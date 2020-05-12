using Acs9;
using Acs10;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.DApp
{
    public partial class DAppContract
    {
        public override Empty TakeContractProfits(TakeContractProfitsInput input)
        {
            var config = State.ProfitConfig.Value;

            // For Side Chain Dividends Pool.
            var amountForSideChainDividendsPool = input.Amount.Mul(config.DonationPartsPerHundred).Div(100);
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Symbol = input.Symbol,
                Amount = amountForSideChainDividendsPool,
                Spender = State.DividendPoolContract.Value
            });
            State.DividendPoolContract.Donate.Send(new DonateInput
            {
                Symbol = input.Symbol,
                Amount = amountForSideChainDividendsPool
            });

            // For receiver.
            var amountForReceiver = input.Amount.Sub(amountForSideChainDividendsPool);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = State.ProfitReceiver.Value,
                Amount = amountForReceiver,
                Symbol = input.Symbol
            });

            // For Token Holder Profit Scheme. (To distribute.)
            State.TokenHolderContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeManager = Context.Self
            });
            return new Empty();
        }

        public override ProfitConfig GetProfitConfig(Empty input)
        {
            return State.ProfitConfig.Value;
        }

        public override ProfitsMap GetProfitsAmount(Empty input)
        {
            var profitsMap = new ProfitsMap();
            foreach (var symbol in State.ProfitConfig.Value.ProfitsTokenSymbolList)
            {
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = symbol
                }).Balance;
                profitsMap.Value[symbol] = balance;
            }

            return profitsMap;
        }
    }
}