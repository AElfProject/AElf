namespace AElf.Kernel
{
    public interface IBlockHeader:ISerializable
    {
        IHash<IMerkleTree<ITransaction>> GetTransactionMerkleTreeRoot();
        void AddTransaction(IHash<ITransaction> hash);
    }
}