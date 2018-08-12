using Newtonsoft.Json.Linq;
using AElf.Kernel;

namespace AElf.ChainController.Rpc
{
    internal static class DtoExtensions
    {
        internal static JObject GetTransactionInfo(this ITransaction tx)
        {
            return new JObject
            {
                ["tx"] = new JObject
                {
                    {"TxId", tx.GetHash().ToHex()},
                    {"From", tx.From.ToHex()},
                    {"To", tx.To.ToHex()},
                    {"Method", tx.MethodName},
                    {"IncrementId", tx.IncrementId}
                }
            };
        }
    }
}