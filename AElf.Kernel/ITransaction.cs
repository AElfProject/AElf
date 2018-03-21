namespace AElf.Kernel
{
    public interface ITransaction 
    {
        /// <summary>
        /// Get hash of the transaction
        /// </summary>
        /// <returns></returns>
        IHash GetHash();

        /// <summary>
        /// When a transaction was created, it should record the last block on the blockchain.
        /// </summary>
        /// <returns></returns>
        IHash LastBlockHashWhenCreating();

        /// <summary>
        /// Get parallel meta data
        /// </summary>
        /// <returns></returns>
        ITransactionParallelMetaData GetParallelMetaData();

        /// <summary>
        /// Method name
        /// </summary>
        string MethodName { get; set; }

        /// <summary>
        /// Params
        /// </summary>
        object[] Params { get; set; }

        /// <summary>
        /// The caller
        /// </summary>
        IAccount From { get; set; }

        /// <summary>
        /// The instrance of a smart contract
        /// </summary>
        IAccount To { get; set; }

        ulong IncrementId { get; set; }
    }

}