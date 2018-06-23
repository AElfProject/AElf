// ReSharper disable once CheckNamespace

using Google.Protobuf;

namespace AElf.Kernel.Types
{
    public partial class SmartContractRegistration : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
       
    }
}