using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Indexing.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Application
{
    internal class CrossChainService : ICrossChainService
    {
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;
        public ILogger<CrossChainService> Logger { get; set; }

        public CrossChainService(ICrossChainCacheEntityService crossChainCacheEntityService, 
            ICrossChainIndexingDataService crossChainIndexingDataService)
        {
            _crossChainCacheEntityService = crossChainCacheEntityService;
            _crossChainIndexingDataService = crossChainIndexingDataService;
        }

        public IOptionsMonitor<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }

        public async Task FinishInitialSyncAsync()
        {
            CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored = false;
            // var isReadyToCreateChainCache =
            //     await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            // if (!isReadyToCreateChainCache)
            //     return;
            // var libIdHeight = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
            var chainIdHeightPairs =
                await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();
            foreach (var chainIdHeight in chainIdHeightPairs.IdHeightDict)
            {
                // register new chain
                _crossChainCacheEntityService.RegisterNewChain(chainIdHeight.Key, chainIdHeight.Value);
            }
        }

        // public Dictionary<int, long> GetNeededChainIdAndHeightPairs()
        // {
        //     var chainIdList = _crossChainCacheEntityService.GetCachedChainIds();
        //     var dict = new Dictionary<int, long>();
        //     foreach (var chainId in chainIdList)
        //     {
        //         var neededChainHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
        //         if (neededChainHeight < Constants.GenesisBlockHeight)
        //             continue;
        //         dict.Add(chainId, neededChainHeight);
        //     }
        //
        //     return dict;
        // }

        // public async Task<Block> GetNonIndexedBlockAsync(long height)
        // {
        //     return await _irreversibleBlockStateProvider.GetNotIndexedIrreversibleBlockByHeightAsync(height);
        // }

        // public async Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId)
        // {
        //     var libDto = await _irreversibleBlockStateProvider.GetLastIrreversibleBlockHashAndHeightAsync();
        //     return await _readerFactory.Create(libDto.BlockHash, libDto.BlockHeight).GetChainInitializationData
        //         .CallAsync(new SInt32Value
        //         {
        //             Value = chainId
        //         });
        // }

        public async Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight)
        {
            if (CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored
                || blockHeight <= Constants.GenesisBlockHeight)
                return;

            _crossChainIndexingDataService.UpdateCrossChainDataWithLib(blockHash, blockHeight);

            var chainIdHeightPairs =
                await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();

            await _crossChainCacheEntityService.UpdateCrossChainCacheAsync(blockHash, blockHeight, chainIdHeightPairs);

            // foreach (var chainIdHeight in chainIdHeightPairs.IdHeightDict)
            // {
            //     // register new chain
            //     _crossChainCacheEntityService.RegisterNewChain(chainIdHeight.Key, chainIdHeight.Value);
            //
            //     // clear cross chain cache
            //     _crossChainCacheEntityService.ClearOutOfDateCrossChainCache(chainIdHeight.Key, chainIdHeight.Value);
            //     Logger.LogDebug(
            //         $"Clear chain {ChainHelper.ConvertChainIdToBase58(chainIdHeight.Key)} cache by height {chainIdHeight.Value}");
            // }
        }

        // private async Task<SideChainIdAndHeightDict> GetAllChainIdHeightPairsAsync(Hash blockHash, long blockHeight)
        // {
        //     return await _readerFactory.Create(blockHash, blockHeight).GetAllChainsIdAndHeight
        //         .CallAsync(new Empty());
        // }
    }
}