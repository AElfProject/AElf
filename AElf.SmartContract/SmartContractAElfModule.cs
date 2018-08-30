using AElf.Common.Module;
using Autofac;

namespace AElf.SmartContract
{
    public class SmartContractAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new SmartContractAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}