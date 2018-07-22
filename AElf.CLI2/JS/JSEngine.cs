using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AElf.CLI2.Commands;
using AElf.CLI2.JS.IO;
using ChakraCore.NET;
using ChakraCore.NET.API;
using ChakraCore.NET.Hosting;
using ServiceStack;
using Console = System.Console;

namespace AElf.CLI2.JS
{
    public class JSEngine : IJSEngine
    {
        private class JSObj : IJSObject
        {
            private JSValue _value;

            public JSObj(JSValue value)
            {
                _value = value;
            }

            public IJSObject Get(string name)
            {
                return new JSObj(_value.ReadProperty<JSValue>(name));
            }


            public TResult Invoke<T, TResult>(string methodName, T arg)
            {
                return _value.CallFunction<T, TResult>(methodName, arg);
            }

            public TResult Invoke<TResult>(string methodName)
            {
                return _value.CallFunction<TResult>(methodName);
            }
        }

        private readonly IConsole _console;
        private readonly ChakraContext _context;
        private readonly BaseOption _option;

        public JSEngine(IConsole console, BaseOption option)
        {
            _console = console;
            _context = JavaScriptHosting.Default.CreateContext(new JavaScriptHostingConfig());
            _option = option;
            ExposeConsoleToContext();
            ExposeAElfOption();
            LoadBridgeJS();
        }

        private void LoadBridgeJS()
        {
            using (var reader = new StreamReader(
                Assembly.GetEntryAssembly().GetManifestResourceStream("AElf.CLI2.Scripts.bridge.js"),
                Encoding.UTF8))
            {
                _context.RunScript(reader.ReadToEnd());
            }
        }

        private void ExposeAElfOption()
        {
            _context.RunScript("var aelf_config = new Object()");
            var config = _context.GlobalObject.ReadProperty<JSValue>("aelf_config");
            config.WriteProperty<string>("server_addr", _option.ServerAddr);
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

        public IJSObject Get(string name)
        {
            return new JSObj(_context.GlobalObject).Get(name);
        }

        public TResult Invoke<T, TResult>(string methodName, T arg)
        {
            return new JSObj(_context.GlobalObject).Invoke<T, TResult>(methodName, arg);
        }

        public TResult Invoke<TResult>(string methodName)
        {
            return new JSObj(_context.GlobalObject).Invoke<TResult>(methodName);
        }
    }
}