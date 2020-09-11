using AElf.Standards.ACS2;
using System.Linq;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

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
                    var resourceInfo = new ResourceInfo
                    {
                        WritePaths =
                        {
                            GetPath(nameof(TokenContractState.Balances), txn.From.ToString(), args.Symbol),
                            GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol),
                        },
                        ReadPaths =
                        {
                            GetPath(nameof(TokenContractState.TokenInfos), args.Symbol),
                            GetPath(nameof(TokenContractState.ChainPrimaryTokenSymbol))
                        }
                    };

                    AddPathForTransactionFee(resourceInfo, txn.From);
                    return resourceInfo;
                }

                case nameof(TransferFrom):
                {
                    var args = TransferFromInput.Parser.ParseFrom(txn.Params);
                    var resourceInfo = new ResourceInfo
                    {
                        WritePaths =
                        {
                            GetPath(nameof(TokenContractState.Allowances), args.From.ToString(), txn.From.ToString(),
                                args.Symbol),
                            GetPath(nameof(TokenContractState.Balances), args.From.ToString(), args.Symbol),
                            GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol),
                            GetPath(nameof(TokenContractState.LockWhiteLists), args.Symbol, txn.From.ToString()),
                        },
                        ReadPaths =
                        {
                            GetPath(nameof(TokenContractState.TokenInfos), args.Symbol),
                            GetPath(nameof(TokenContractState.ChainPrimaryTokenSymbol))
                        }
                    };
                    AddPathForTransactionFee(resourceInfo, txn.From);
                    return resourceInfo;
                }

                default:
                    return new ResourceInfo {NonParallelizable = true};
            }
        }

        private void AddPathForTransactionFee(ResourceInfo resourceInfo, Address from)
        {
            var symbols = GetMethodFeeSymbols();
            var primaryTokenSymbol = GetPrimaryTokenSymbol(new Empty()).Value;
            if (_primaryTokenSymbol != string.Empty && !symbols.Contains(primaryTokenSymbol))
                symbols.Add(primaryTokenSymbol);
            var paths = symbols.Select(symbol => GetPath(nameof(TokenContractState.Balances), from.ToString(), symbol));
            foreach (var path in paths)
            {
                if(resourceInfo.WritePaths.Contains(path)) continue;
                resourceInfo.WritePaths.Add(path);
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