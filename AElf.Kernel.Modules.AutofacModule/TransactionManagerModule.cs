using AElf.Kernel.Managers;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class TransactionManagerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TransactionManager>().As<ITransactionManager>();
        }
    }
}