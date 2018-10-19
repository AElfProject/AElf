using AElf.Common.Module;
using Autofac;

namespace AElf.Synchronization
{
    public class SyncAElfModule : IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new SyncAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
            throw new System.NotImplementedException();
        }
    }
}