using AElf.Kernel;
using AElf.Common;

namespace AElf.ChainController
{
    public interface IChainService
    {
        IBlockChain GetBlockChain(Hash chainId);
        ILightChain GetLightChain(Hash chainId);
        bool IsBlockReceived(Hash blockHash, ulong height);
    }
}