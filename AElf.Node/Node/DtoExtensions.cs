using Newtonsoft.Json.Linq;

namespace AElf.Kernel.Node
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
                    {"IncrementId", tx.IncrementId}
                }
            };
        }
    }
}