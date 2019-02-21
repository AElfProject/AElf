using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        
        public CrossChainService(ICrossChainDataProvider crossChainDataProvider)
        {
            _crossChainDataProvider = crossChainDataProvider;
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
            return await _crossChainDataProvider.GetSideChainBlockData(sideChainBlockData, true);
        }
        
        public async Task<bool> ValidateParentChainBlockData(IList<ParentChainBlockData> parentChainBlockData)
        {
            return await _crossChainDataProvider.GetParentChainBlockData(parentChainBlockData, true);
        }
    }
}