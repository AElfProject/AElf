using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel;

[DependsOn(
    typeof(DatabaseAElfModule),
    typeof(CoreAElfModule)
)]
public class CoreKernelAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddTransient<ITransactionResultQueryService, TransactionResultService>();
        services.AddTransient<IBlockValidationProvider, SystemTransactionValidationProvider>();

        services.AddTransient(typeof(IStoreKeyPrefixProvider<>), typeof(StoreKeyPrefixProvider<>));

        services.AddStoreKeyPrefixProvide<BlockBody>("bb");
        services.AddStoreKeyPrefixProvide<BlockHeader>("bh");
        services.AddStoreKeyPrefixProvide<BlockStateSet>("bs");
        services.AddStoreKeyPrefixProvide<Chain>("ch");
        services.AddStoreKeyPrefixProvide<ChainBlockLink>("cl");
        services.AddStoreKeyPrefixProvide<ChainBlockIndex>("ci");
        services.AddStoreKeyPrefixProvide<ChainStateInfo>("cs");
        services.AddStoreKeyPrefixProvide<Transaction>("tx");
        services.AddStoreKeyPrefixProvide<TransactionBlockIndex>("ti");
        services.AddStoreKeyPrefixProvide<TransactionResult>("tr");
        services.AddStoreKeyPrefixProvide<VersionedState>("vs");
        services.AddStoreKeyPrefixProvide<InvalidTransactionResult>("ir");

        services.AddTransient(typeof(IStateStore<>), typeof(StateStore<>));
        services.AddSingleton(typeof(INotModifiedCachedStateStore<>), typeof(NotModifiedCachedStateStore<>));
        services.AddTransient(typeof(IBlockchainStore<>), typeof(BlockchainStore<>));
        services.AddSingleton(typeof(ICachedBlockchainExecutedDataService<>),
            typeof(CachedBlockchainExecutedDataService<>));
        services.AddSingleton(typeof(IBlockchainExecutedDataCacheProvider<>),
            typeof(BlockchainExecutedDataCacheProvider<>));

        services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseRedisDatabase());
        services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseRedisDatabase());
    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        var transactionBlockIndexService =
            context.ServiceProvider.GetRequiredService<ITransactionBlockIndexService>();
        AsyncHelper.RunSync(transactionBlockIndexService.LoadTransactionBlockIndexAsync);
    }
}

public static class StoreKeyPrefixProviderServiceCollectionExtensions
{
    public static IServiceCollection AddStoreKeyPrefixProvide<T>(
        this IServiceCollection serviceCollection, string prefix)
        where T : IMessage<T>, new()
    {
        serviceCollection.AddTransient<IStoreKeyPrefixProvider<T>>(c =>
            new FastStoreKeyPrefixProvider<T>(prefix));

        return serviceCollection;
    }
}