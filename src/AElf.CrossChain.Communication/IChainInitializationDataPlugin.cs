using System.Threading.Tasks;
using Acs7;

namespace AElf.CrossChain.Communication
{
    public interface IChainInitializationDataPlugin
    {
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
    }
}