using AElf.Kernel;

namespace AElf.ChainController
{
    public interface IChainService
    {
        IBlockChain GetBlockChain(Hash chainId);

        ILightChain GetLightChain(Hash chainId);
    }
}