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

        public async Task<List<SideChainBlockData>> GetSideChainBlockData()
        {
            var res = new List<SideChainBlockData>();
            await _crossChainDataProvider.GetSideChainBlockData(res);
            return res;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockData()
        {
            var res = new List<ParentChainBlockData>();
            await _crossChainDataProvider.GetParentChainBlockData(res);
            return res;
        }

        public async Task<bool> ValidateSideChainBlockData(IList<SideChainBlockData> sideChainBlockData)
        {
            return await _crossChainDataProvider.GetSideChainBlockData(sideChainBlockData);
        }
        
        public async Task<bool> ValidateParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData)
        {
            return await _crossChainDataProvider.GetParentChainBlockData(parentChainBlockData);
        }
       
    }
}