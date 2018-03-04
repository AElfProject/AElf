namespace AElf.Kernel
{
    public interface IChainContextFactory
    {
        IChainContext GetChainContext(IHash<IChain> chain);
    }
}