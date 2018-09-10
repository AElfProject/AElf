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
                    {"Method", tx.MethodName},
                    {"IncrementId", tx.IncrementId}
                }
            };
        }
    }
}