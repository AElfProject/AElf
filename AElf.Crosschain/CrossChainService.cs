using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.BlockService;

namespace AElf.Crosschain
{
    public class CrossChainService : ICrossChainService, IBlockExtraDataProvider
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;

        private delegate void NewSideChainHandler(IClientBase clientBase);

        private readonly NewSideChainHandler _newSideChainHandler;
        public CrossChainService(ICrossChainDataProvider crossChainDataProvider, IClientManager clientManager)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _newSideChainHandler += clientManager.CreateClient;
            _newSideChainHandler += _crossChainDataProvider.AddNewSideChainCache;
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

        public async Task FillExtraData(Block block)
        {
            var sideChainBlockData = await GetSideChainBlockInfo();
            var sideChainTransactionsRoot = new BinaryMerkleTree()
                .AddNodes(sideChainBlockData.Select(scb => scb.TransactionMKRoot).ToArray()).ComputeRootHash();
            block.Header.BlockExtraData.SideChainTransactionsRoot = sideChainTransactionsRoot;
            block.Body.SideChainBlockData.AddRange(sideChainBlockData);
            
            var parentChainBlockData = await GetParentChainBlockInfo();
            block.Body.ParentChainBlockData.AddRange(parentChainBlockData);
        }
    }
}