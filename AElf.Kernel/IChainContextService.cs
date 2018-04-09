namespace AElf.Kernel
{
    public interface IChainContextService
    {
        IChainContext GetChainContext(IHash chainId);
    }
}