using AElf.Kernel.Managers;
using Autofac;
using AElf.SmartContract;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class StateDictatorModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StateDictator>().As<IStateDictator>().SingleInstance();
        }
    }
}