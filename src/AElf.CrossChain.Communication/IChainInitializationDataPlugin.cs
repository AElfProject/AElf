using System.Threading.Tasks;
using Acs7;
using AElf.Kernel.Node.Infrastructure;
using Google.Protobuf;

namespace AElf.CrossChain.Communication
{
    public interface IChainInitializationDataPlugin
    {
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
    }
}