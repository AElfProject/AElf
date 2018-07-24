using AElf.CLI2.JS.Crypto;
using AElf.CLI2.JS.IO;
using Autofac;
using ChakraCore.NET.Debug;

namespace AElf.CLI2.JS
{
    public class JSModule : Module
    {
        private readonly IBridgeJSProvider _bridgeJSProvider;

        public JSModule(IBridgeJSProvider bridgeJsProvider)
        {
            _bridgeJSProvider = bridgeJsProvider;
        }


        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Console>().As<IConsole>();
            builder.RegisterType<JSEngine>().As<IJSEngine>();
            builder.RegisterInstance(_bridgeJSProvider).As<IBridgeJSProvider>();
            builder.RegisterType<PseudoRandomGenerator>().As<IRandomGenerator>();
            builder.RegisterType<JSDebugAdapter>().As<IDebugAdapter>();
            base.Load(builder);
        }
    }
}