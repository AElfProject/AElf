using AElf.ChainController.CrossChain;
using AElf.Kernel;
using Autofac;

namespace AElf.ChainController
{
    public class ChainAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly = typeof(BlockValidationService).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<ChainCreationService>().As<IChainCreationService>();
            builder.RegisterType<ChainContextService>().As<IChainContextService>();
            builder.RegisterType<ChainService>().As<IChainService>().SingleInstance();
            builder.RegisterType<CrossChainInfo>().As<ICrossChainInfo>().SingleInstance();
        }
    }
}