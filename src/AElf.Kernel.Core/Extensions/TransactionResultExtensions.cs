using System.Linq;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public static class TransactionResultExtensions
    {
        public static void UpdateBloom(this TransactionResult transactionResult)
        {
            var bloom = new Bloom();
            bloom.Combine(transactionResult.Logs.Where(l => !l.Name.StartsWith("__")).Select(l => l.GetBloom()));
            transactionResult.Bloom = ByteString.CopyFrom(bloom.Data);
        }
    }
}