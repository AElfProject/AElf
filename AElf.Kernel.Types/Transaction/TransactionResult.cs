using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionResult : ISerializable
    {
        public void UpdateBloom()
        {
            var bloom = new Bloom();
            foreach (var le in Logs)
            {
                bloom.AddValue(le.Address);
                foreach (var t in le.Topics)
                {
                    bloom.AddValue(t.ToByteArray());
                }
            }

            Bloom = ByteString.CopyFrom(bloom.Data);
        }
        
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}