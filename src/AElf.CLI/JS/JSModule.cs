using AElf.CLI.JS.Crypto;
using AElf.CLI.JS.IO;
using Autofac;
using ChakraCore.NET.Debug;

namespace AElf.CLI.JS
{
    public class JSModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Console>().As<IConsole>();
            builder.RegisterType<JSEngine>().As<IJSEngine>();
            builder.RegisterType<RequestExecutor>().As<IRequestExecutor>();
            builder.RegisterType<PseudoRandomGenerator>().As<IRandomGenerator>();
            builder.RegisterType<JSDebugAdapter>().As<IDebugAdapter>();
            base.Load(builder);
        }
    }
}