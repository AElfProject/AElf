using AElf.Common.Module;
using Autofac;

namespace AElf.Network
{
    public class NetworkAElfModule:IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new NetworkAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}