// ReSharper disable once CheckNamespace

using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class SmartContractRegistration 
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
       
    }
}