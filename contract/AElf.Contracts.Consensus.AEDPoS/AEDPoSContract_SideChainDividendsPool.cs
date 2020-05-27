using System.Linq;
using Acs10;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenHolder;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using ContributeProfitsInput = AElf.Contracts.TokenHolder.ContributeProfitsInput;
using DistributeProfitsInput = AElf.Contracts.TokenHolder.DistributeProfitsInput;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private void InitialProfitSchemeForSideChain(long periodSeconds)
        {
            var tokenHolderContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
            // No need to continue if Token Holder Contract didn't deployed.
            if (tokenHolderContractAddress == null)
            {
                Context.LogDebug(() => "Token Holder Contract not found, so won't initial side chain dividends pool.");
                return;
            }

            State.TokenHolderContract.Value = tokenHolderContractAddress;
            State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
            {
                Symbol = AEDPoSContractConstants.SideChainShareProfitsTokenSymbol,
                MinimumLockMinutes = periodSeconds.Div(60)
            });

            Context.LogDebug(() => "Side chain dividends pool created.");
        }

        public override Empty Donate(DonateInput input)
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

            Context.Fire(new DonationReceived
            {
                From = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount,
                PoolContract = Context.Self
            });

            var currentReceivedDividends = State.SideChainReceivedDividends[Context.CurrentHeight];
            if (currentReceivedDividends != null && currentReceivedDividends.Value.ContainsKey(input.Symbol))
            {
                currentReceivedDividends.Value[input.Symbol] =
                    currentReceivedDividends.Value[input.Symbol].Add(input.Amount);
            }
            else
            {
                currentReceivedDividends = new Dividends
                {
                    Value =
                    {
                        {
                            input.Symbol, input.Amount
                        }
                    }
                };
            }

            State.SideChainReceivedDividends[Context.CurrentHeight] = currentReceivedDividends;

            Context.LogDebug(() => $"Contributed {input.Amount} {input.Symbol}s to side chain dividends pool.");

            return new Empty();
        }

        public override Empty Release(ReleaseInput input)
        {
            Assert(false, "Side chain dividend pool can only release automatically.");
            return new Empty();
        }

        public void Release()
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

        public override Empty SetSymbolList(SymbolList input)
        {
            Assert(false, "Side chain dividend pool not support setting symbol list.");
            return new Empty();
        }

        public override Dividends GetDividends(Int64Value input)
        {
            Assert(Context.CurrentHeight > input.Value, "Cannot query dividends of a future block.");
            return State.SideChainReceivedDividends[input.Value];
        }

        public override SymbolList GetSymbolList(Empty input)
        {
            return new SymbolList
            {
                Value =
                {
                    GetSideChainDividendPoolScheme().ReceivedTokenSymbols
                }
            };
        }

        public override Dividends GetUndistributedDividends(Empty input)
        {
            var scheme = GetSideChainDividendPoolScheme();
            return new Dividends
            {
                Value =
                {
                    scheme.ReceivedTokenSymbols.Select(s => State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = scheme.VirtualAddress,
                        Symbol = s
                    })).ToDictionary(b => b.Symbol, b => b.Balance)
                }
            };
        }

        private Scheme GetSideChainDividendPoolScheme()
        {
            if (State.SideChainDividendPoolSchemeId.Value == null)
            {
                var tokenHolderScheme = State.TokenHolderContract.GetScheme.Call(Context.Self);
                State.SideChainDividendPoolSchemeId.Value = tokenHolderScheme.SchemeId;
            }

            return Context.Call<Scheme>(
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
                nameof(ProfitContractContainer.ProfitContractReferenceState.GetScheme),
                State.SideChainDividendPoolSchemeId.Value);
        }
    }
}