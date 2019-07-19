using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain
{
    public class CrossChainService : ICrossChainService
    {
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;
        private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;
//        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        private readonly IReaderFactory _readerFactory;

        internal CrossChainService(IIrreversibleBlockStateProvider irreversibleBlockStateProvider, 
            ICrossChainCacheEntityProvider crossChainCacheEntityProvider, 
            ICrossChainIndexingDataService crossChainIndexingDataService, IReaderFactory readerFactory)
        {
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
            _crossChainCacheEntityProvider = crossChainCacheEntityProvider;
            _crossChainIndexingDataService = crossChainIndexingDataService;
            _readerFactory = readerFactory;
        }

        public IOptionsMonitor<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }

        public async Task FinishInitialSyncAsync()
        {
            CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored = false;
            var isReadyToCreateChainCache = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            if (!isReadyToCreateChainCache)
                return;
            var libIdHeight = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            _ = RegisterNewChainsAsync(libIdHeight.BlockHash, libIdHeight.BlockHeight);
        }
        
        public List<int> GetRegisteredChainIdList()
        {
            return _crossChainCacheEntityProvider.GetCachedChainIds();
        }

        public long GetNeededChainHeight(int chainId)
        {
            return _crossChainCacheEntityProvider.GetChainCacheEntity(chainId).TargetChainHeight();
        }

        public async Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight)
        {
            if (!CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored)
            {
                await RegisterNewChainsAsync(blockHash, blockHeight);
            }
            
            _crossChainIndexingDataService.UpdateCrossChainDataWithLibIndex(new BlockIndex
            {
                Hash = blockHash,
                Height = blockHeight
            });
        }

        public async Task<Block> GetNonIndexedBlockAsync(long height)
        {
            return await _irreversibleBlockStateProvider.GetIrreversibleBlockByHeightAsync(height);
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            var chainInitializationData =
                await _crossChainIndexingDataService.GetChainInitializationDataAsync(chainId, libDto.BlockHash,
                    libDto.BlockHeight);
            return chainInitializationData;
        }

        private async Task RegisterNewChainsAsync(Hash blockHash, long blockHeight)
        {
            var sideChainIdHeightPairs = await GetAllChainIdHeightPairsAsync(blockHash, blockHeight);

            foreach (var chainIdHeight in sideChainIdHeightPairs.IdHeightDict)
            {
                _crossChainCacheEntityProvider.AddChainCacheEntity(chainIdHeight.Key, chainIdHeight.Value + 1);
            }
        }
        
        public async Task<SideChainIdAndHeightDict> GetAllChainIdHeightPairsAsync(Hash blockHash, long blockHeight)
        {
            return await _readerFactory.Create(blockHash, blockHeight).GetAllChainsIdAndHeight
                .CallAsync(new Empty());
        }
    }
}