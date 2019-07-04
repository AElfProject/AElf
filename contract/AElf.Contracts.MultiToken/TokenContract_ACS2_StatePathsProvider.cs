using Acs2;
using AElf.Contracts.MultiToken.Messages;
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
                        Reources =
                        {
                            GetPathHashCode(nameof(TokenContractState.Balances), txn.From.ToString(), args.Symbol),
                            GetPathHashCode(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol)
                        }
                    };
                }

                case nameof(TransferFrom):
                {
                    var args = TransferFromInput.Parser.ParseFrom(txn.Params);
                    return new ResourceInfo
                    {
                        Reources =
                        {
                            GetPathHashCode(nameof(TokenContractState.Allowances), args.From.ToString(),
                                txn.From.ToString(),
                                args.Symbol),
                            GetPathHashCode(nameof(TokenContractState.Balances), args.From.ToString(), args.Symbol),
                            GetPathHashCode(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol)
                        }
                    };
                }

                case nameof(DonateResourceToken):
                {
                    return new ResourceInfo
                    {
                        Reources =
                        {
                            GetPathHashCode(nameof(TokenContractState.ChargedResources), "CPU"),
                            GetPathHashCode(nameof(TokenContractState.ChargedResources), "STO"),
                            GetPathHashCode(nameof(TokenContractState.ChargedResources), "NET"),
                            GetPathHashCode(nameof(TokenContractState.TreasuryContract))
                        }
                    };
                }

                case nameof(ClaimTransactionFees):
                {
                    return GetClaimTransactionFessResourceInfo();
                }

                // TODO: Support more methods
                default:
                    throw new AssertionException($"invalid method: {txn.MethodName}");
            }
        }

        private ResourceInfo GetClaimTransactionFessResourceInfo()
        {
            var resourceInfo = new ResourceInfo
            {
                Reources =
                {
                    GetPathHashCode(nameof(TokenContractState.PreviousBlockTransactionFeeTokenSymbolList)),
                    GetPathHashCode(nameof(TokenContractState.TreasuryContract))
                }
            };
            if (State.PreviousBlockTransactionFeeTokenSymbolList.Value != null)
            {
                foreach (var symbol in State.PreviousBlockTransactionFeeTokenSymbolList.Value.SymbolList)
                {
                    resourceInfo.Reources.Add(GetPathHashCode(nameof(TokenContractState.ChargedFees), symbol));
                }
            }

            return resourceInfo;
        }

        private int GetPathHashCode(params string[] parts)
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
            }.GetHashCode();
        }
    }
}