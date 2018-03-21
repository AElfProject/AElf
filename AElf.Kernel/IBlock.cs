namespace AElf.Kernel
{
    /// <summary>
    /// Define block, it cointains all transactions in memory
    /// </summary>
    public interface IBlock
    {
        Hash GetHash();
        IBlockHeader GetHeader();
        IBlockBody GetBody();
        bool AddTransaction(IHash tx);
    }
}