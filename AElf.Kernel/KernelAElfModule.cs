using AElf.Common;
using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.Network;
using AElf.Database;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;

using Microsoft.Extensions.Logging;

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
            var services = context.Services;

            services.AddAssemblyOf<KernelAElfModule>();
            
            services.AddTransient(
                typeof(ISerializer<>), 
                typeof(Serializer<>));

            services.AddTransient(
                typeof(IEqualityIndex<>), 
                typeof(EqualityIndex<,>));
            
            services.AddTransient(
                typeof(IComparisionIndex<>), 
                typeof(ComparisionIndex<,>));
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.SingleNode)
            {
                GlobalConfig.BlockProducerNumber = 1;
            }

            
            //TODO! change log output 
            
            var loggerFactory = context.ServiceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddNLog();

            //builder.RegisterModule(new LoggerAutofacModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
        }
    }
}