using AElf.Kernel.Concurrency.Metadata;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MetadataModule : Module
    {

        public Hash ChainId { get; set; }
        
        protected override void Load(ContainerBuilder builder)
        {

            var kk = builder.Properties.Keys;
            builder.RegisterType(typeof(FunctionMetadataService)).As<IFunctionMetadataService>().SingleInstance();
            
        }
    }
}