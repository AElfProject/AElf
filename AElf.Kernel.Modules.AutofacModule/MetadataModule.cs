using Autofac;
using AElf.SmartContract.Metadata;
using AElf.SmartContract;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MetadataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType(typeof(FunctionMetadataService)).As<IFunctionMetadataService>().SingleInstance();
        }
    }
}