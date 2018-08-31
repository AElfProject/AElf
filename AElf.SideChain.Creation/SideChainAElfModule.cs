using AElf.Common.Module;
using AElf.Kernel;
using Autofac;
using Easy.MessageHub;

namespace AElf.SideChain.Creation
{
    public class SideChainAElfModule:IAElfModlule
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