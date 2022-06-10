using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Communication.Application;

public class CrossChainClientService : ICrossChainClientService, ITransientDependency
{
    private readonly ICrossChainClientProvider _crossChainClientProvider;

    public CrossChainClientService(ICrossChainClientProvider crossChainClientProvider)
    {
        _crossChainClientProvider = crossChainClientProvider;
    }

    public ILogger<CrossChainClientService> Logger { get; set; }

    public async Task<ICrossChainClient> GetConnectedCrossChainClientAsync(int chainId)
    {
        if (!_crossChainClientProvider.TryGetClient(chainId, out var client))
            return null;
        await ConnectAsync(client);
        return !client.IsConnected ? null : client;
    }

    public Task<ICrossChainClient> CreateClientAsync(CrossChainClientCreationContext crossChainClientCreationContext)
    {
        var client = _crossChainClientProvider.AddOrUpdateClient(crossChainClientCreationContext);
        return Task.FromResult(client);
    }

    public Task<ICrossChainClient> CreateChainInitializationClientAsync(int chainId)
    {
        var crossChainClientDto = new CrossChainClientCreationContext
        {
            IsClientToParentChain = true,
            LocalChainId = chainId
        };
        var client = _crossChainClientProvider.CreateChainInitializationDataClient(crossChainClientDto);
        return Task.FromResult(client);
    }

    public async Task CloseClientsAsync()
    {
        var clientList = _crossChainClientProvider.GetAllClients();
        foreach (var client in clientList) await client.CloseAsync();
    }

    private async Task ConnectAsync(ICrossChainClient client)
    {
        if (client.IsConnected)
            return;
        Logger.LogDebug($"Try connect with chain {ChainHelper.ConvertChainIdToBase58(client.RemoteChainId)}");
        await client.ConnectAsync();
    }
}