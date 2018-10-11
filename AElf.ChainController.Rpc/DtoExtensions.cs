using Newtonsoft.Json.Linq;
using AElf.Common;
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
                    {"TxId", tx.GetHash().DumpHex()},
                    {"From", tx.From.DumpHex()},
                    {"To", tx.To.DumpHex()},
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
                res.Add(sideChainIndexedInfo.ChainId.DumpHex(), new JObject
                {
                    {"Height", sideChainIndexedInfo.Height},
                    {"BlockHash", sideChainIndexedInfo.BlockHeaderHash.DumpHex()},
                    {"TransactionMerkleTreeRoot", sideChainIndexedInfo.TransactionMKRoot.DumpHex()}
                });
            }

            return res;
        }
    }
}