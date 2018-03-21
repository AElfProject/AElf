using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public interface IBlockHeader
    {
        IHash GetTransactionMerkleTreeRoot();
        void AddTransaction(IHash hash);
    }
}