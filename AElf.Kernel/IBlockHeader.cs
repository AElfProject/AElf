namespace AElf.Kernel
{
    public interface IBlockHeader
    {
        IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot();
        IHash<IMerkleTree<IAccount>> GetStateMerkleTreeRoot();
        void AddTransaction(IHash<ITransaction> hash);
    }
}