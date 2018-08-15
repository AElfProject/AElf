using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class StorageModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DataStore>().As<IDataStore>();
        }
    }
}