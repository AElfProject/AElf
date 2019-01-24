using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionResult 
    {        
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}