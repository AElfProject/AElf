using AElf.Kernel.Storages;
using AElf.SmartContract.Metadata;
using Autofac;

namespace AElf.SmartContract
{
    public class SmartContractAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly1 = typeof(IDataProvider).Assembly;
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            
            var assembly2 = typeof(DataProvider).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();
            
            builder.RegisterType<SmartContractService>().As<ISmartContractService>().SingleInstance();
            builder.RegisterType<FunctionMetadataService>().As<IFunctionMetadataService>().SingleInstance();
            builder.RegisterType<StateStore>().As<IStateStore>().SingleInstance();
        }
    }
}