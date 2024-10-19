using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Cache.Application;
using AElf.CrossChain.Communication.Application;
using AElf.ExceptionHandler;
using AElf.Standards.ACS7;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Application;

public partial class CrossChainRequestService : ICrossChainRequestService, ITransientDependency
{
    private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;
    private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
    private readonly ICrossChainClientService _crossChainClientService;

    public CrossChainRequestService(ICrossChainClientService crossChainClientService,
        ICrossChainCacheEntityService crossChainCacheEntityService,
        IBlockCacheEntityProducer blockCacheEntityProducer)
    {
        _crossChainClientService = crossChainClientService;
        _crossChainCacheEntityService = crossChainCacheEntityService;
        _blockCacheEntityProducer = blockCacheEntityProducer;
    }

    public ILogger<CrossChainRequestService> Logger { get; set; }

    public async Task RequestCrossChainDataFromOtherChainsAsync()
    {
        var chainIdHeightDict = GetNeededChainIdAndHeightPairs();

        foreach (var chainIdHeightPair in chainIdHeightDict)
        {
            var chainIdBased58 = ChainHelper.ConvertChainIdToBase58(chainIdHeightPair.Key);
            Logger.LogDebug(
                $"Try to request from chain {chainIdBased58}, from height {chainIdHeightPair.Value}");
            await RequestCrossChainDataAsync(chainIdHeightPair);
        }
    }

    [ExceptionHandler(typeof(CrossChainRequestException), TargetType = typeof(CrossChainRequestService),
        MethodName = nameof(HandleExceptionWhileRequestingCrossChainData))]
    private async Task RequestCrossChainDataAsync(KeyValuePair<int, long> chainIdHeightPair)
    {
        var client = await _crossChainClientService.GetConnectedCrossChainClientAsync(chainIdHeightPair.Key);
        if (client != null)
            await client.RequestCrossChainDataAsync(chainIdHeightPair.Value,
                b => _blockCacheEntityProducer.TryAddBlockCacheEntity(b));
    }

    public async Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId)
    {
        Logger.LogDebug("Request chain initialization data.");
        var client = await _crossChainClientService.CreateChainInitializationClientAsync(chainId);
        return await client.RequestChainInitializationDataAsync(chainId);
    }

    private Dictionary<int, long> GetNeededChainIdAndHeightPairs()
    {
        var chainIdList = _crossChainCacheEntityService.GetCachedChainIds();
        var dict = new Dictionary<int, long>();
        foreach (var chainId in chainIdList)
        {
            var neededChainHeight = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
            if (neededChainHeight < AElfConstants.GenesisBlockHeight)
                continue;
            dict.Add(chainId, neededChainHeight);
        }

        return dict;
    }
}