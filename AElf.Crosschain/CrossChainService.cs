using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IClientManager _clientManager;

        public CrossChainService(ICrossChainDataProvider crossChainDataProvider, IClientManager clientManager)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _clientManager = clientManager;
            //Todo: event listener for new side chain.
        }

        public async Task<List<SideChainBlockInfo>> GetSideChainBlockInfo()
        {
            var res = new List<SideChainBlockInfo>();
            await _crossChainDataProvider.GetSideChainBlockInfo(res);
            return res;
        }

        public async Task<List<ParentChainBlockInfo>> GetParentChainBlockInfo()
        {
            var res = new List<ParentChainBlockInfo>();
            await _crossChainDataProvider.GetParentChainBlockInfo(res);
            return res;
        }

        public async Task<bool> ValidateSideChainBlockInfo(List<SideChainBlockInfo> sideChainBlockInfo)
        {
            return await _crossChainDataProvider.GetSideChainBlockInfo(sideChainBlockInfo);
        }

        public async Task<bool> ValidateParentChainBlockInfo(List<ParentChainBlockInfo> parentChainBlockInfo)
        {
            return await _crossChainDataProvider.GetParentChainBlockInfo(parentChainBlockInfo);
        }

        public void IndexNewSideChain(IClientBase clientBase)
        {
            _clientManager.CreateClient(clientBase);
            _crossChainDataProvider.AddNewSideChainCache(clientBase.BlockInfoCache);
        }
    }
}