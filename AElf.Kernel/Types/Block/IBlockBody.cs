using Google.Protobuf.Collections;

namespace AElf.Kernel
{
    public interface IBlockBody
    {
        RepeatedField<Hash> Transactions { get; }

        bool AddTransaction(Hash tx);
    }
}