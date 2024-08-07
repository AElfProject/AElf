using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.Services;

public interface ISolangEndpointProvider
{
    Task SetSolangEndpointAsync(IBlockIndex blockIndex, string limit);
    Task<string> GetSolangEndpointAsync(IBlockIndex blockIndex);
}

public class SolangEndpointProvider : BlockExecutedDataBaseProvider<string>,
    ISolangEndpointProvider, ISingletonDependency
{
    private const string BlockExecutedDataName = "SolangEndpoint";

    public SolangEndpointProvider(
        ICachedBlockchainExecutedDataService<string> cachedBlockchainExecutedDataService) : base(
        cachedBlockchainExecutedDataService)
    {
        Logger = NullLogger<SolangEndpointProvider>.Instance;
    }

    public ILogger<SolangEndpointProvider> Logger { get; set; }

    public Task<string> GetSolangEndpointAsync(IBlockIndex blockIndex)
    {
        var solangEndpoint = GetBlockExecutedData(blockIndex);
        return Task.FromResult(solangEndpoint);
    }

    public async Task SetSolangEndpointAsync(IBlockIndex blockIndex, string solangEndpoint)
    {
        // var blockTransactionLimit = new string();
        // blockTransactionLimit.Value = SolangEndpoint;
        await AddBlockExecutedDataAsync(blockIndex, solangEndpoint);
        Logger.LogDebug($"SolangEndpoint has been changed to {solangEndpoint}");
    }

    protected override string GetBlockExecutedDataName()
    {
        return BlockExecutedDataName;
    }
}