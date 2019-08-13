using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain
{
    internal class CrossChainService : ICrossChainService
    {
        private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly IReaderFactory _readerFactory;

        public CrossChainService(IIrreversibleBlockStateProvider irreversibleBlockStateProvider, 
            IReaderFactory readerFactory, ICrossChainCacheEntityService crossChainCacheEntityService)
        {
            _irreversibleBlockStateProvider = irreversibleBlockStateProvider;
            _readerFactory = readerFactory;
            _crossChainCacheEntityService = crossChainCacheEntityService;
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

        public Dictionary<int, long> GetNeededChainIdAndHeightPairs()
        {
            var chainIdList = _crossChainCacheEntityService.GetCachedChainIds();
            var dict = new Dictionary<int, long>();
            foreach (var chainId in chainIdList)
            {
                var neededChainHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
                dict.Add(chainId, neededChainHeight);
            }

            return dict;
        }

        public async Task<Block> GetNonIndexedBlockAsync(long height)
        {
            return await _irreversibleBlockStateProvider.GetIrreversibleBlockByHeightAsync(height);
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        {
            var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            return await _readerFactory.Create(libDto.BlockHash, libDto.BlockHeight).GetChainInitializationData.CallAsync(new SInt32Value
            {
                Value = chainId
            });
        }
        
        private async Task<SideChainIdAndHeightDict> GetAllChainIdHeightPairsAsync(Hash blockHash, long blockHeight)
        {
            return await _readerFactory.Create(blockHash, blockHeight).GetAllChainsIdAndHeight
                .CallAsync(new Empty());
        }
        
        public async Task RegisterNewChainsAsync(Hash blockHash, long blockHeight)
        {
            if (CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored
                || blockHeight <= Constants.GenesisBlockHeight)
                return;
            
            var sideChainIdHeightPairs = await GetAllChainIdHeightPairsAsync(blockHash, blockHeight);

            foreach (var chainIdHeightPair in sideChainIdHeightPairs.IdHeightDict)
            {
                _crossChainCacheEntityService.RegisterNewChain(chainIdHeightPair.Key, chainIdHeightPair.Value);
            }
        }
    }
}