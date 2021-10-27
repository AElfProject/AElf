using System.Threading.Tasks;
using AElf.Standards.ACS7;

namespace AElf.CrossChain.Application
{
    public interface ICrossChainResponseService
    {
        Task<SideChainBlockData> ResponseSideChainBlockDataAsync(long requestHeight);
        Task<ParentChainBlockData> ResponseParentChainBlockDataAsync(long requestHeight, int remoteSideChainId);

        Task<ChainInitializationData> ResponseChainInitializationDataFromParentChainAsync(int chainId);
    }
}