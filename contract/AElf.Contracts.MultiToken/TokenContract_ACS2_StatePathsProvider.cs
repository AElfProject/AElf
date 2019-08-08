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

                // TODO: Support more methods
                default:
                    throw new AssertionException($"invalid method: {txn.MethodName}");
            }
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