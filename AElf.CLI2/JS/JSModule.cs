using AElf.CLI2.JS.IO;
using Autofac;

namespace AElf.CLI2.JS
{
    public class JSModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Console>().As<IConsole>();
            builder.RegisterType<JSEngine>().As<IJSEngine>();
            base.Load(builder);
        }
    }
}