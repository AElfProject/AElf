using AElf.Kernel.Managers;
using Autofac;
using Org.BouncyCastle.Crypto.Tls;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class WorldStateManagerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WorldStateConsole>().As<IWorldStateConsole>().SingleInstance();
        }
    }
}