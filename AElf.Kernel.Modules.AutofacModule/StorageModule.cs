using AElf.Kernel.Node.RPC;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class StorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WorldStateStore>().As<IWorldStateStore>();
            builder.RegisterType<DataStore>().As<IDataStore>();
            builder.RegisterType<ChangesStore>().As<IChangesStore>();
            builder.RegisterType<SmartContractStore>().As<ISmartContractStore>();
            builder.RegisterType<TransactionResultStore>().As<TransactionResultStore>();
        }
    }
}