using AElf.Kernel.Orleans.Core;
using Orleans;
using Orleans.Hosting;

namespace AElf.Kernel.SmartContract.Orleans.Application;

public class OrleansTransactionExecutingClientService : IOrleansTransactionExecutingClientService
{
    private readonly IClusterClient _client;

    public OrleansTransactionExecutingClientService()
    {
        _client = new ClientBuilder()
            .UseLocalhostClustering()
            .AddSimpleMessageStreamProvider("SMSProvider")
            .Build();
    }

    public async Task ExecuteGrain(string grainKey, TransactionContext transactionContext)
    {
        await using (_client)
        {
            await _client.Connect();
            var grain = _client.GetGrain<ISmartContractGrain>(grainKey);
            await grain.ExecuteAsync(transactionContext);
        }
    }
}