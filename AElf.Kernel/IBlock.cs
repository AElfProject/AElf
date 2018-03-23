namespace AElf.Kernel
{
    /// <summary>
    /// Define block, it cointains all transactions in memory
    /// </summary>
    public interface IBlock
    {
        Hash GetHash();
        bool AddTransaction(Hash tx);
        
        IBlockHeader Header { get; set; }
        
        IBlockBody Body { get; set; }
    }
}