using AElf.Common;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.Network;
using AElf.Modularity;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;

using Microsoft.Extensions.Logging;

namespace AElf.Kernel
{
    public class KernelAElfModule: AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddConventionalRegistrar(new AElfKernelConventionalRegistrar());
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {


            var services = context.Services;
            var assembly1 = typeof(ISerializer<>).Assembly;

            services.AddAssemblyOf<KernelAElfModule>();
            
            /*
            
            
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            
            var assembly2 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            
            builder.RegisterType<SmartContractManager>().As<ISmartContractManager>();
            builder.RegisterType<TransactionManager>().As<ITransactionManager>();
            builder.RegisterType<TransactionResultManager>().As<ITransactionResultManager>();
            builder.RegisterType<HashManager>().As<IHashManager>();
            builder.RegisterType<BlockManagerBasic>().As<IBlockManagerBasic>();
            builder.RegisterType<ChainManagerBasic>().As<IChainManagerBasic>();
            builder.RegisterType<BinaryMerkleTreeManager>().As<IBinaryMerkleTreeManager>();
            builder.RegisterType<DataStore>().As<IDataStore>();
            builder.RegisterType<MinersManager>().As<IMinersManager>();*/
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            if (ConsensusConfig.Instance.ConsensusType == ConsensusType.SingleNode)
            {
                GlobalConfig.BlockProducerNumber = 1;
            }

            
            //TODO! change log output 
            
            //context.ServiceProvider.GetService<ILoggerFactory>();

            //builder.RegisterModule(new LoggerAutofacModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
        }
    }
}