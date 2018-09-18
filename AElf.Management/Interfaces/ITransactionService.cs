namespace AElf.Management.Interfaces
{
    public interface ITransactionService
    {
        ulong GetPoolSize(string chainId);
    }
}