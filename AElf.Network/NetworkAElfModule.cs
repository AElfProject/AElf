using AElf.Common.Module;
using Autofac;

namespace AElf.Network
{
    public class NetworkAElfModule:IAElfModlule
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