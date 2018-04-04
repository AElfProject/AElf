namespace AElf.Kernel
{
    /// <summary>
    /// Define block, it cointains all transactions in memory
    /// </summary>
    public interface IBlock
    {
        Hash GetHash();
        bool AddTransaction(Hash tx);
        
        BlockHeader Header { get; set; }
        
        BlockBody Body { get; set; }
    }
}