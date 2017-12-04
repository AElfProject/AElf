namespace AElf.Kernel
{
    public interface ITransaction
    {
        /// <summary>
        /// Get hash of the transaction
        /// </summary>
        /// <returns></returns>
        IHash<ITransaction> GetHash();

        /// <summary>
        /// When a transaction was created, it should record the last block on the blockchain.
        /// </summary>
        /// <returns></returns>
        IHash<IBlockHeader> LastBlockHashWhenCreating();
    }
}