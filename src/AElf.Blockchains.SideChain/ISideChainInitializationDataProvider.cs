using System.Threading.Tasks;
using Acs7;

namespace AElf.Blockchains.SideChain
{
    public interface ISideChainInitializationDataProvider
    {
        Task<ChainInitializationData> GetChainInitializationDataAsync();
        int ParentChainId { get; }
    }
}