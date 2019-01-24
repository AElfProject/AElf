using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class SmartContractDeployment 
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}