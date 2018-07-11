using AElf.Kernel.Managers;
using Autofac;
using AElf.SmartContract;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class WorldStateDictatorModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<WorldStateDictator>().As<IWorldStateDictator>().SingleInstance();
        }
    }
}