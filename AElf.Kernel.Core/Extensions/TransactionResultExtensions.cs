using Google.Protobuf;

namespace AElf.Kernel
{
    public static class TransactionResultExtensions
    {
        public static void UpdateBloom(this TransactionResult transactionResult)
        {
            if (transactionResult.Logs.Count == 0)
            {
                return;
            }
            var bloom = new Bloom();
            foreach (var le in transactionResult.Logs)
            {
                bloom.AddValue(le.Address);
                foreach (var t in le.Indexed)
                {
                    bloom.AddValue(t.ToByteArray());
                }
            }

            transactionResult.Bloom = ByteString.CopyFrom(bloom.Data);
        }
    }
}