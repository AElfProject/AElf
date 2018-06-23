using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class SmartContractDeployment : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}