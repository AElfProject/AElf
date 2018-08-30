using AElf.Common.Module;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new KernelAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}