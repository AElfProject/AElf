using AElf.Common.Extensions;
using AElf.Kernel;
using Newtonsoft.Json.Linq;

namespace AElf.Node.AElfChain
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
                    {"Method", tx.MethodName},
                    {"IncrementId", tx.IncrementId},
                    {"RefBlockNumber", tx.RefBlockNumber},
                    {"RefBlockPrefix", tx.RefBlockPrefix.ToByteArray().ToHex()},
                    {"Type", tx.Type.ToString()}
                }
            };
        }
        
        /*internal static JObject GetIndexedSideChainBlcokInfo(this IBlockHeader blockHeader)
        {
            var res = new JObject();
            foreach (var sideChainIndexedInfo in blockHeader.IndexedInfo)
            {
                res.Add(sideChainIndexedInfo.ChainId.ToHex(), new JObject
                {
                    {"Height", sideChainIndexedInfo.Height},
                    {"BlockHash", sideChainIndexedInfo.BlockHeaderHash.ToHex()},
                    {"TransactionMerkleTreeRoot", sideChainIndexedInfo.TransactionMKRoot.ToHex()}
                });
            }

            return res;
        }*/
    }
}