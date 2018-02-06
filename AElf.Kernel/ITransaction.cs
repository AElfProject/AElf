namespace AElf.Kernel
{
    public interface ITransaction : ISerializable
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
        byte[] From { get; set; }

        /// <summary>
        /// The instrance of a smart contract
        /// </summary>
        byte[] To { get; set; }

        ulong IncrementId { get; set; }
    }

}