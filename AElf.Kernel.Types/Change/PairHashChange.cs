using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class PairHashChange : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}