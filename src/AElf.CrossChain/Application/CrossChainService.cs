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

        public IOptions<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }

        public async Task FinishInitialSyncAsync()
        {
            CrossChainConfigOptions.Value.CrossChainDataValidationIgnored = false;
            var chainIdHeightPairs =
                await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();
            foreach (var chainIdHeight in chainIdHeightPairs.IdHeightDict)
            {
                // register new chain
                _crossChainCacheEntityService.RegisterNewChain(chainIdHeight.Key, chainIdHeight.Value);
            }
        }

        public async Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight)
        {
            if (CrossChainConfigOptions.Value.CrossChainDataValidationIgnored
                || blockHeight <= AElfConstants.GenesisBlockHeight)
                return;

            _crossChainIndexingDataService.UpdateCrossChainDataWithLib(blockHash, blockHeight);

            var chainIdHeightPairs =
                await _crossChainIndexingDataService.GetAllChainIdHeightPairsAtLibAsync();

            await _crossChainCacheEntityService.UpdateCrossChainCacheAsync(blockHash, blockHeight, chainIdHeightPairs);
        }
    }
}