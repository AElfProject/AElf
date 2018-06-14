using AElf.Kernel.Concurrency.Metadata;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MetadataModule : Module
    {
        public MetadataModule(Hash chainId)
        {
            ChainId = chainId;
        }

        public Hash ChainId { get; set; }
        
        protected override void Load(ContainerBuilder builder)
        {
            if (ChainId == null)
            {
                return;
            }

            builder.RegisterType(typeof(ChainFunctionMetadataTemplate)).As<IChainFunctionMetadataTemplate>()
                .WithParameter("chainId", ChainId);
            builder.RegisterType(typeof(ChainFunctionMetadata)).As<IChainFunctionMetadata>();
        }
    }
}