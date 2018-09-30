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
                    {"TxId", tx.GetHash().ToHex()},
                    {"From", tx.From.ToHex()},
                    {"To", tx.To.ToHex()},
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
                res.Add(sideChainIndexedInfo.ChainId.ToHex(), new JObject
                {
                    {"Height", sideChainIndexedInfo.Height},
                    {"BlockHash", sideChainIndexedInfo.BlockHeaderHash.ToHex()},
                    {"TransactionMerkleTreeRoot", sideChainIndexedInfo.TransactionMKRoot.ToHex()}
                });
            }

            return res;
        }
    }
}