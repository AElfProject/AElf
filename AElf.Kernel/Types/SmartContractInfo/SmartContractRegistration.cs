// ReSharper disable once CheckNamespace
using AElf.Database;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class SmartContractRegistration : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
       
    }
}