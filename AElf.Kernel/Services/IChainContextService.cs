namespace AElf.Kernel.Services
{
    public interface IChainContextService
    {
        IChainContext GetChainContext(Hash chainId);
    }
}