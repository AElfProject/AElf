using AElf.Common.Extensions;
using Newtonsoft.Json.Linq;
using AElf.Kernel;

namespace AElf.ChainController.Rpc
{
    internal static class DtoExtensions
    {
        internal static JObject GetTransactionInfo(this Transaction tx)
        {
            return new JObject
            {
                ["tx"] = new JObject
                {
                    {"TxId", tx.GetHash().Dumps()},
                    {"From", tx.From.Dumps()},
                    {"To", tx.To.Dumps()},
                    {"RefBlockNumber", tx.RefBlockNumber},
                    {"RefBlockPrefix", tx.RefBlockPrefix.ToByteArray().ToHex()},
                    {"Method", tx.MethodName}
                }
            };
        }
        
        internal static JObject GetIndexedSideChainBlcokInfo(this IBlock block)
        {
            var res = new JObject();
            foreach (var sideChainIndexedInfo in block.Body.IndexedInfo)
            {
                res.Add(sideChainIndexedInfo.ChainId.Dumps(), new JObject
                {
                    {"Height", sideChainIndexedInfo.Height},
                    {"BlockHash", sideChainIndexedInfo.BlockHeaderHash.Dumps()},
                    {"TransactionMerkleTreeRoot", sideChainIndexedInfo.TransactionMKRoot.Dumps()}
                });
            }

            return res;
        }
    }
}