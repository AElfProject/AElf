namespace AElf.ChainController.TxMemPool
{
    public interface IContractTxPool : IPool
    {
        /// <summary>
        /// the maximal number of tx in one block
        /// </summary>
        ulong Least { get; }
        
        /// <summary>
        /// the minimal number of tx in one block
        /// </summary>
        ulong Limit { get; }

        void SetBlockVolume(ulong minimal, ulong maximal);
    }
}