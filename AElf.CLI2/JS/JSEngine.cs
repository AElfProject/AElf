using System;
using System.Collections.Generic;
using System.IO;
using AElf.CLI2.JS.IO;
using AElf.Kernel;
using Autofac.Core.Activators;
using ChakraCore.NET;
using ChakraCore.NET.API;
using ChakraCore.NET.Hosting;

namespace AElf.CLI2.JS
{
    public class JSEngine : IJSEngine
    {
        private IConsole _console;
        private ChakraContext _context;


        public JSEngine(IConsole console)
        {
            _console = console;
            _context = JavaScriptHosting.Default.CreateContext(new JavaScriptHostingConfig());
            ExposeConsoleToContext();
        }

        private void ExposeConsoleToContext()
        {
            _context.ServiceNode.GetService<IJSValueConverterService>()
                .RegisterProxyConverter<IConsole>(
                    (binding, instance, serviceNode) =>
                    {
                        Action<string, Action<JavaScriptValue>> setMethod = binding.SetMethod<JavaScriptValue>;
                        setMethod("debug", instance.Debug);
                        setMethod("log", instance.Log);
                        setMethod("warn", instance.Warn);
                        setMethod("error", instance.Error);
                    });
            _context.GlobalObject.WriteProperty<IConsole>("console", _console);
        }

        public void RunScriptFile(string filename)
        {
            _context.RunScript(File.ReadAllText(filename));
        }
    }
}