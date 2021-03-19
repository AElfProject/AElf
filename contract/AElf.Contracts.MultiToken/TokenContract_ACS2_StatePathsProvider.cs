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
                        }
                    };

                    return resourceInfo;
                }

                default:
                    return new ResourceInfo {NonParallelizable = true};
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