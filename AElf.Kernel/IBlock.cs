namespace AElf.Kernel
{
    /// <summary>
    /// Define block, it cointains all transactions in memory
    /// </summary>
    public interface IBlock
    {
        IHash<IBlock> GetHash();
        IBlockHeader GetHeader();
        IBlockBody GetBody();
        bool AddTransaction(ITransaction tx);
    }
}