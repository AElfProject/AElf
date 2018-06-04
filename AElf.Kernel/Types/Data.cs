using AElf.Database;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Data : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}