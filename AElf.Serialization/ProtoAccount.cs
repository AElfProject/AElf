using AElf.Kernel;
using Google.Protobuf;

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