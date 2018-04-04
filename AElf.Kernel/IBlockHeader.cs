using AElf.Kernel.Merkle;

namespace AElf.Kernel
{
    public interface IBlockHeader
    {
        Hash GetTransactionMerkleTreeRoot();
        void AddTransaction(Hash hash);

        Hash PreviousHash { get; set; }
        
        Hash Hash { get;}
    }
}