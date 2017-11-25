namespace AElf.Kernel
{
    /// <summary>
    /// Define block, it cointains all transactions in memory
    /// </summary>
    public interface IBlock
    {
        IHash GetHash();
        IBlockHeader GetHeader();
        IBlockBody GetBody();
        void AddTransaction(ITransaction tx);
    }
}