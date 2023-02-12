using System.Threading.Tasks;
using AElf.Standards.ACS7;

namespace AElf.CrossChain.Application;

public interface ISideChainInitializationDataProvider
{
    int ParentChainId { get; }
    Task<ChainInitializationData> GetChainInitializationDataAsync();
}