using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AElf.CLI2.JS.IO;
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


        private static JavaScriptValue ToJSMethod(IServiceNode node, Action<IEnumerable<JavaScriptValue>> a)
        {
            node.GetService<IJSValueConverterService>();
            IJSValueService jsValueService = node.GetService<IJSValueService>();
            return jsValueService.CreateFunction(
                (JavaScriptNativeFunction) ((callee, isConstructCall, arguments, argumentCount, callbackData) =>
                {
                    a(arguments.Skip(1));
                    return jsValueService.JSValue_Undefined;
                }), IntPtr.Zero);
        }

        private void ExposeConsoleToContext()
        {
            _context.ServiceNode.GetService<IJSValueConverterService>()
                .RegisterProxyConverter<IConsole>(
                    (binding, instance, serviceNode) =>
                    {
                        // The ChakraCore.Net public API cannot register a variadic method. Use refactor to register a
                        // variadic method below
                        var valServ = serviceNode.GetService<IJSValueService>();
                        var valConvServ = serviceNode.GetService<IJSValueConverterService>();
                        valConvServ.RegisterConverter<Action<IEnumerable<JavaScriptValue>>>(ToJSMethod, null);
                        var val = (JavaScriptValue) typeof(JSValueBinding)
                            .GetField("jsValue", BindingFlags.NonPublic | BindingFlags.Instance)
                            .GetValue(binding);
                        Action<string, Action<IEnumerable<JavaScriptValue>>> setMethod = (name, fn) =>
                        {
                            valServ.WriteProperty<Action<IEnumerable<JavaScriptValue>>>(val, name, fn);
                        };
                        foreach (var methodInfo in typeof(IConsole).GetMethods())
                        {
                            setMethod(methodInfo.Name.ToLower(),
                                (args) => { methodInfo.Invoke(instance, new object[] {args}); });
                        }
                    });
            _context.GlobalObject.WriteProperty<IConsole>("console", _console);
        }

        public void RunScriptFile(string filename)
        {
            _context.RunScript(File.ReadAllText(filename));
        }
    }
}