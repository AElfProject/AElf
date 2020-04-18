using System.Threading.Tasks;
using Acs7;

namespace AElf.Blockchains.ContractInitialization
{
    public interface ISideChainInitializationDataProvider
    {
        Task<ChainInitializationData> GetChainInitializationDataAsync();
        int ParentChainId { get; }
    }
}