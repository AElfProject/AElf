namespace AElf.Kernel
{
    public interface IGenesisBlock : IBlock
    {
        ITransaction TransactionInit { get; }
    }
}