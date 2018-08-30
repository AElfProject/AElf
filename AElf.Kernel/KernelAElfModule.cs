using AElf.Common;
using AElf.Common.Module;
using AElf.Configuration.Config.Network;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new KernelAutofacModule());
            builder.RegisterModule(new LoggerAutofacModule("aelf-node-" + NetworkConfig.Instance.ListeningPort));
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}