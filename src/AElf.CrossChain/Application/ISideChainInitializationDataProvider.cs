using System.Threading.Tasks;
using AElf.Standards.ACS7;

namespace AElf.CrossChain.Application
{
    public interface ISideChainInitializationDataProvider
    {
        Task<ChainInitializationData> GetChainInitializationDataAsync();
        int ParentChainId { get; }
    }
}