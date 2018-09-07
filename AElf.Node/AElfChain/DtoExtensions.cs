using AElf.Kernel;
using Google.Protobuf;
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
                    {"Type", tx.Type.ToString()}
                }
            };
        }
        
        internal static JObject GetIndexedSideChainBlcokInfo(this IBlockHeader blockHeader)
        {
            var res = new JObject();
            foreach (var sideChainIndexedInfo in blockHeader.IndexedInfo)
            {
                res.Add(new JObject(JsonFormatter.Default.Format(sideChainIndexedInfo)));
            }

            return res;
        }
    }
}