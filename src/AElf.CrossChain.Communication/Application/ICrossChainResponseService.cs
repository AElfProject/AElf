using System.Threading.Tasks;
using Acs7;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainResponseService
    {
        Task<SideChainBlockData> ResponseSideChainBlockDataAsync(long requestHeight);
        Task<ParentChainBlockData> ResponseParentChainBlockDataAsync(long requestHeight, int remoteSideChainId);

        Task<ChainInitializationData> ResponseChainInitializationDataFromParentChainAsync(int chainId);
    }
}