using AElf.Common.Module;
using Autofac;

namespace AElf.SmartContract
{
    public class SmartContractAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            var assembly1 = typeof(IStateDictator).Assembly;
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            
            var assembly2 = typeof(StateDictator).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();
        }

        public void Run(ILifetimeScope scope)
        {
            throw new System.NotImplementedException();
        }
    }
}