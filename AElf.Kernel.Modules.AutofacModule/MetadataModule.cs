using AElf.Kernel.Concurrency.Metadata;
using Autofac;

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