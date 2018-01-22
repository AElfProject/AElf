namespace AElf.Kernel
{
    public interface ITransaction
    {
        IAccount AccountFrom { get; set; }

        IAccount AccountTo { get; set; }

        int Amount { get; set; }

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

        /// <summary>
        /// Get parallel meta data
        /// </summary>
        /// <returns></returns>
        ITransactionParallelMetaData GetParallelMetaData();
    }
}