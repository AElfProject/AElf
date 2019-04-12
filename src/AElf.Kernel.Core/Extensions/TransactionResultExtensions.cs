using System.Linq;
using Google.Protobuf;

namespace AElf.Kernel
{
    public static class TransactionResultExtensions
    {
        public static void UpdateBloom(this TransactionResult transactionResult)
        {
            // TODO: What should be the default value for bloom if there is no log
//            if (transactionResult.Logs.Count == 0)
//            {
//                return;
//            }
            var bloom = new Bloom();
            bloom.Combine(transactionResult.Logs.Select(l=>l.GetBloom()));
            transactionResult.Bloom = ByteString.CopyFrom(bloom.Data);
        }
    }
}