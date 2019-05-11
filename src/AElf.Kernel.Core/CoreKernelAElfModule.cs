using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(DatabaseAElfModule), typeof(CoreAElfModule))]
    public class CoreKernelAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddConventionalRegistrar(new AElfKernelConventionalRegistrar());
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddAssemblyOf<CoreKernelAElfModule>();
            services.AddTransient<ITransactionResultQueryService, TransactionResultService>();

            services.AddTransient(typeof(IStoreKeyPrefixProvider<>), typeof(StoreKeyPrefixProvider<>));

            services.AddStoreKeyPrefixProvide<BlockBody>("bb");
            services.AddStoreKeyPrefixProvide<BlockHeader>("bh");
            services.AddStoreKeyPrefixProvide<BlockStateSet>("bss");
            services.AddStoreKeyPrefixProvide<Chain>("c");
            services.AddStoreKeyPrefixProvide<ChainBlockLink>("cbl");
            services.AddStoreKeyPrefixProvide<ChainBlockIndex>("cbi");
            services.AddStoreKeyPrefixProvide<ChainStateInfo>("csi");
            services.AddStoreKeyPrefixProvide<FunctionMetadata>("fm");
            services.AddStoreKeyPrefixProvide<SerializedCallGraph>("scg");
            services.AddStoreKeyPrefixProvide<Transaction>("t");
            services.AddStoreKeyPrefixProvide<TransactionBlockIndex>("tbi");
            services.AddStoreKeyPrefixProvide<TransactionResult>("tr");
            services.AddStoreKeyPrefixProvide<TransactionReceipt>("trc");
            services.AddStoreKeyPrefixProvide<VersionedState>("vs");
            

            services.AddTransient(typeof(IStateStore<>), typeof(StateStore<>));
            services.AddSingleton(typeof(INotModifiedCachedStateStore<>), typeof(NotModifiedCachedStateStore<>));
            services.AddTransient(typeof(IBlockchainStore<>), typeof(BlockchainStore<>));

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseRedisDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseRedisDatabase());

            services.AddTransient<IBlockValidationProvider, BlockValidationProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
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
}