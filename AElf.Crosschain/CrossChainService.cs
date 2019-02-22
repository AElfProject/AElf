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

        public async Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(int chainId)  
        {
            var res = new List<SideChainBlockData>();
            await _crossChainDataProvider.GetSideChainBlockDataAsync(chainId, res);
            return res;
        }

        public async Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(int chainId)
        {
            var res = new List<ParentChainBlockData>();
            await _crossChainDataProvider.GetParentChainBlockDataAsync(chainId, res);
            return res;
        }

        public async Task<bool> ValidateSideChainBlockDataAsync(int chainId, IList<SideChainBlockData> sideChainBlockData)
        {
            return await _crossChainDataProvider.GetSideChainBlockDataAsync(chainId, sideChainBlockData, true);
        }
        
        public async Task<bool> ValidateParentChainBlockDataAsync(int chainId, IList<ParentChainBlockData> parentChainBlockData)
        {
            return await _crossChainDataProvider.GetParentChainBlockDataAsync(chainId, parentChainBlockData, true);
        }
    }
}