using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContractExecution.Infrastructure;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(TypesAElfModule), typeof(DatabaseAElfModule), typeof(CoreAElfModule))]
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

            services.AddTransient<IByteSerializer, ProtobufSerializer>();

            services.AddTransient(typeof(IStateStore<>), typeof(StateStore<>));
            services.AddTransient(typeof(IBlockchainStore<>), typeof(BlockchainStore<>));
            services.AddSingleton(typeof(IChainRelatedComponentManager<>), typeof(ChainRelatedComponentManager<>));

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseRedisDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseRedisDatabase());

            services.AddTransient<IBlockValidationProvider, BlockValidationProvider>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}