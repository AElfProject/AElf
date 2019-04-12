using System.Runtime.CompilerServices;
using AElf.Common;
using AElf.Kernel;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("AElf.OS.Rpc.ChainController.Tests")]
namespace AElf.OS.Rpc.ChainController
{
    internal static class DtoExtensions
    {
        internal static JObject GetTransactionInfo(this Transaction tx)
        {
            return new JObject
            {
                ["Transaction"] = new JObject
                {
                    {"Id", tx.GetHash().ToHex()},
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