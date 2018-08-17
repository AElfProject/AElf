using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class StorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CanonicalHashStore>().As<ICanonicalHashStore>();
            builder.RegisterType<CurrentHashStore>().As<ICurrentHashStore>();
            builder.RegisterType<GenesisHashStore>().As<IGenesisHashStore>();
            builder.RegisterType<DataStore>().As<IDataStore>();
        }
    }
}