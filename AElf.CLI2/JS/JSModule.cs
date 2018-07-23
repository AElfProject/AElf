using AElf.CLI2.JS.IO;
using Autofac;

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
            base.Load(builder);
        }
    }
}