using Google.Protobuf;

namespace AElf.Kernel.Types
{
    public partial class SmartContractDeployment : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}