using AElf.Kernel;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Serialization.ProtoMessages
{
    public partial class ProtoAccount: Account 
    {
        public override byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}