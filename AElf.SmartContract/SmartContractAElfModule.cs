using AElf.Kernel;
using AElf.Modularity;
using AElf.SmartContract.Consensus;
using AElf.SmartContract.Proposal;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.SmartContract
{
    [DependsOn(typeof(KernelAElfModule))]
    public class SmartContractAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            

            context.Services.AddAssemblyOf<SmartContractAElfModule>();



            context.Services.AddSingleton<IAuthorizationInfo,AuthorizationInfo>();
            context.Services.AddSingleton<IElectionInfo,ElectionInfo>();


            /*
             
            var assembly1 = typeof(IDataProvider).Assembly;
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            
            var assembly2 = typeof(DataProvider).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();
            
            builder.RegisterType<SmartContractService>().As<ISmartContractService>().SingleInstance();
            builder.RegisterType<FunctionMetadataService>().As<IFunctionMetadataService>().SingleInstance();
            builder.RegisterType<StateStore>().As<IStateStore>().SingleInstance();
            builder.RegisterType<AuthorizationInfo>().As<IAuthorizationInfo>().SingleInstance();
            builder.RegisterType<ElectionInfo>().As<IElectionInfo>().SingleInstance();
            
            */
        }

    }
}