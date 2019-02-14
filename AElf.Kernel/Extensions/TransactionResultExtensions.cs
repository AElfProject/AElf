using Google.Protobuf;

namespace AElf.Kernel.Extensions
{
    public static class TransactionResultExtensions
    {
        public static void UpdateBloom(this TransactionResult transactionResult)
        {
            var bloom = new Bloom();
            foreach (var le in transactionResult.Logs)
            {
                bloom.AddValue(le.Address);
                foreach (var t in le.Topics)
                {
                    bloom.AddValue(t.ToByteArray());
                }
            }

            transactionResult.Bloom = ByteString.CopyFrom(bloom.Data);
        }
    }
}