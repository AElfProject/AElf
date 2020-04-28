using System.Threading.Tasks;
using Acs7;

namespace AElf.CrossChain.Application
{
    public interface ISideChainInitializationDataProvider
    {
        Task<ChainInitializationData> GetChainInitializationDataAsync();
        int ParentChainId { get; }
    }
}