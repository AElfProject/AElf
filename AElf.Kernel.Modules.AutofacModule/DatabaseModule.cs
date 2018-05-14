using AElf.Database;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class DatabaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<KeyValueDatabase>().As<IKeyValueDatabase>();
        }
    }
}