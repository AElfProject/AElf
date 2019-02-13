using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Crosschain.Grpc;
using AElf.Kernel;
using AElf.Kernel.BlockService;
using Volo.Abp.EventBus;

namespace AElf.Crosschain
{
    public class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;

        private delegate void NewSideChainHandler(IClientBase clientBase);

        private readonly NewSideChainHandler _newSideChainHandler;

        public CrossChainService(ICrossChainDataProvider crossChainDataProvider, IClientService clientService)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _newSideChainHandler += clientService.CreateClient;
            _newSideChainHandler += _crossChainDataProvider.AddNewSideChainCache;
        }

        public async Task<List<SideChainBlockData>> GetSideChainBlockInfo()
        {
            var res = new List<SideChainBlockData>();
            await _crossChainDataProvider.GetSideChainBlockInfo(res);
            return res;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockInfo()
        {
            var res = new List<ParentChainBlockData>();
            await _crossChainDataProvider.GetParentChainBlockInfo(res);
            return res;
        }

        public async Task<bool> ValidateSideChainBlockInfo(List<SideChainBlockData> sideChainBlockInfo)
        {
            return await _crossChainDataProvider.GetSideChainBlockInfo(sideChainBlockInfo);
        }
        
        public async Task<bool> ValidateParentChainBlockInfo(List<ParentChainBlockData> parentChainBlockInfo)
        {
            return await _crossChainDataProvider.GetParentChainBlockInfo(parentChainBlockInfo);
        }
       
    }
}