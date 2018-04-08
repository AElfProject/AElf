namespace AElf.Kernel
{
    public interface IAccountContextService
    {
        IAccountDataContext GetAccountDataContext(Hash accountHash, Hash chainId);
    }
}