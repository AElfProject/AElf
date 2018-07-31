using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class TransactionManagerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TransactionStore>().As<TransactionStore>();
            builder.RegisterType<TransactionManager>().As<TransactionManager>();
        }
    }
}