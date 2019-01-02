using AElf.Common;

namespace AElf.Kernel
{
    public interface IChainService
    {
        IBlockChain GetBlockChain(Hash chainId);
        ILightChain GetLightChain(Hash chainId);
    }
}