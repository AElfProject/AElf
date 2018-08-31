using AElf.Common.Module;
using Autofac;

namespace AElf.SmartContract
{
    public class SmartContractAElfModule:IAElfModule
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