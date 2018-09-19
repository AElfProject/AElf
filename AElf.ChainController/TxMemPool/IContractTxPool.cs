namespace AElf.ChainController.TxMemPool
{
    public interface IContractTxPool : IPool
    {
        /// <summary>
        /// the maximal number of tx in one block
        /// </summary>
        int Least { get; }
        
        /// <summary>
        /// the minimal number of tx in one block
        /// </summary>
        int Limit { get; }

        void SetBlockVolume(int minimal, int maximal);
    }
}