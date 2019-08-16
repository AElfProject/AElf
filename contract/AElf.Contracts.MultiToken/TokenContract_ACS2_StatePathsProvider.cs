using System.Collections.Generic;
using System.Linq;
using Acs2;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override ResourceInfo GetResourceInfo(Transaction txn)
        {
            switch (txn.MethodName)
            {
                case nameof(Transfer):
                {
                    var args = TransferInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(TokenContractState.Balances), txn.From.ToString(), args.Symbol),
                            GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol)
                        }
                    };
                }

                case nameof(TransferFrom):
                {
                    var args = TransferFromInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(TokenContractState.Allowances), args.From.ToString(), txn.From.ToString(),
                                args.Symbol),
                            GetPath(nameof(TokenContractState.Balances), args.From.ToString(), args.Symbol),
                            GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol)
                        }
                    };
                }

                case nameof(DonateResourceToken):
                {
                    return GetDonateResourceTokenResourceInfo(txn);
                }

                case nameof(ClaimTransactionFees):
                {
                    return GetClaimTransactionFessResourceInfo(txn);
                }

                // TODO: Support more methods
                default:
                    throw new AssertionException($"invalid method: {txn.MethodName}");
            }
        }

        private ResourceInfo GetDonateResourceTokenResourceInfo(Transaction txn)
        {
            var resourceInfo = new ResourceInfo
            {
                Paths =
                {
                    GetPath(nameof(TokenContractState.TreasuryContract))
                }
            };

            foreach (var symbol in TokenContractConstants.ResourceTokenSymbols.Except(new List<string> {"RAM"}))
            {
                resourceInfo.Paths.Add(GetPath(nameof(TokenContractState.ChargedResources), symbol));
            }

            return resourceInfo;
        }

        private ResourceInfo GetClaimTransactionFessResourceInfo(Transaction txn)
        {
            var resourceInfo = new ResourceInfo
            {
                Paths =
                {
                    GetPath(nameof(TokenContractState.PreviousBlockTransactionFeeTokenSymbolList)),
                    GetPath(nameof(TokenContractState.TreasuryContract))
                }
            };
            if (State.PreviousBlockTransactionFeeTokenSymbolList.Value == null) return resourceInfo;
            foreach (var symbol in State.PreviousBlockTransactionFeeTokenSymbolList.Value.SymbolList)
            {
                resourceInfo.Paths.Add(GetPath(nameof(TokenContractState.ChargedFees), symbol));
            }

            return resourceInfo;
        }

        private ScopedStatePath GetPath(params string[] parts)
        {
            // TODO: Use more sophisticated algorithm than GetHashCode
            return new ScopedStatePath
            {
                Address = Context.Self,
                Path = new StatePath
                {
                    Parts =
                    {
                        parts
                    }
                }
            };
        }
    }
}