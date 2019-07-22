using AElf.Types;
using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public interface IBlockBody : IHashProvider
    {
        RepeatedField<Hash> TransactionIds { get; }
    }
}