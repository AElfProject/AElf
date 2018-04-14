namespace AElf.Kernel
{
    public interface IChainContextService
    {
        IChainContext GetChainContext(Hash chainId);
    }
}