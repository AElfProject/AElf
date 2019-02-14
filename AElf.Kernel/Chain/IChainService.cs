namespace AElf.Kernel
{
    public interface IChainService
    {
        IBlockChain GetBlockChain(int chainId);
        ILightChain GetLightChain(int chainId);
    }
}