﻿using AElf.Common;
using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Common.Serializers;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Database;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(TypesAElfModule),typeof(DatabaseAElfModule),typeof(CoreAElfModule))]
    public class KernelAElfModule: AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddConventionalRegistrar(new AElfKernelConventionalRegistrar());
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            // TODO : Maybe it shouldn't be set here
            Configure<ChainOptions>(configuration);

            //Configure<DbConnectionOptions>(configuration);
            
            var services = context.Services;

            services.AddAssemblyOf<KernelAElfModule>();

            services.AddTransient<IByteSerializer, AElf.Common.Serializers.ProtobufSerializer>();
            
            services.AddTransient(
                typeof(IEqualityIndex<>), 
                typeof(EqualityIndex<,>));

            services.AddSingleton<IMinerService, MinerService>();
            
            services.AddTransient(
                typeof(IComparisionIndex<>), 
                typeof(ComparisionIndex<,>));
            
            services.AddTransient(typeof(IStateStore<>), typeof(StateStore<>));
            services.AddTransient(typeof(IBlockchainStore<>), typeof(BlockchainStore<>));

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseRedisDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseRedisDatabase());

        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //TODO! change log output 
            
            var loggerFactory = context.ServiceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddNLog();

            //builder.RegisterModule(new LoggerAutofacModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
        }
    }
}