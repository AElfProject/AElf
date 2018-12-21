using AElf.Kernel;
using AElf.Modularity;
using Autofac;
using Easy.MessageHub;

namespace AElf.SideChain.Creation
{
    public class SideChainAElfModule: AElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterType<ChainCreationEventListener>().PropertiesAutowired();
        }

        public void Run(ILifetimeScope scope)
        {
            var evListener = scope.Resolve<ChainCreationEventListener>();
            MessageHub.Instance.Subscribe<IBlock>(async (t) =>
            {
                await evListener.OnBlockAppended(t);
            });
        }
    }
}