using Acs2;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override StatePathsInfo GetStatePaths(Transaction txn)
        {
            switch (txn.MethodName)
            {
                case nameof(Transfer):
                {
                    var args = TransferInput.Parser.ParseFrom(txn.Params);
                    return new StatePathsInfo
                    {
                        Paths =
                        {
                            GetPath(nameof(TokenContractState.Balances), txn.From.ToString()),
                            GetPath(nameof(TokenContractState.Balances), args.To.ToString())
                        }
                    };
                }

                case nameof(TransferFrom):
                {
                    var args = TransferFromInput.Parser.ParseFrom(txn.Params);
                    return new StatePathsInfo()
                    {
                        Paths =
                        {
                            GetPath(nameof(TokenContractState.Allowances), args.From.ToString(),
                                txn.From.ToString()),
                            GetPath(nameof(TokenContractState.Balances), args.From.ToString()),
                            GetPath(nameof(TokenContractState.Balances), args.To.ToString())
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