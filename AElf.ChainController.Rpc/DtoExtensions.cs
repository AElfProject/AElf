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
                ["Transaction"] = new JObject
                {
                    {"TransactionId", tx.GetHash().ToHex()},
                    {"From", tx.From.GetFormatted()},
                    {"To", tx.To.GetFormatted()},
                    {"RefBlockNumber", tx.RefBlockNumber},
                    {"RefBlockPrefix", tx.RefBlockPrefix.ToByteArray().ToHex()},
                    {"Method", tx.MethodName}
                }
            };
        }
    }
}