using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AElf.CLI2.Commands;
using AElf.CLI2.JS.Crypto;
using AElf.CLI2.JS.IO;
using AElf.CLI2.JS.Net;
using Alba.CsConsoleFormat;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET;
using ChakraCore.NET.API;
using ChakraCore.NET.Debug;
using ChakraCore.NET.Hosting;
using Console = System.Console;

namespace AElf.CLI2.JS
{
    public class JSObj : IJSObject
    {
        private readonly JSValue _value;

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

        public IJSObject InvokeAndGetJSObject(string methodName)
        {
            return new JSObj(_value.CallFunction<JSValue>(methodName));
        }

        public JavaScriptValue Value => _value.ReferenceValue;
    }

    public class JSEngine : IJSEngine
    {
        private readonly IConsole _console;
        private readonly ChakraContext _context;
        private readonly BaseOption _option;
        private readonly IRandomGenerator _randomGenerator;
        private readonly PrettyPrint _prettyPrint;
        private HttpRequestor _requestor;

        public IServiceNode ServiceNode => _context.ServiceNode;
        public JSValue GlobalObject => _context.GlobalObject;

        public JSEngine(IConsole console, BaseOption option,
            IRandomGenerator randomGenerator, IDebugAdapter debugAdapter)
        {
            _console = console;
//            var config = new JavaScriptHostingConfig {DebugAdapter = debugAdapter};
            var config = new JavaScriptHostingConfig();
            _context = JavaScriptHosting.Default.CreateContext(config);
            _context.RegisterEvalService();
            _prettyPrint = new PrettyPrint(this);
            _option = option;
            _randomGenerator = randomGenerator;
            ExposeConsoleToContext();
            ExposeCryptoHelpers();
            ExposeAElfOption();
            LoadCryptoJS();
            LoadAelfJs();
            LoadHelpersJs();
            ExposeHttpRequestorToContext();
        }

        private void LoadAelfJs()
        {
            RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.aelf.js"));
            RunScript(@"Aelf = require('aelf');");
        }

        private void LoadHelpersJs()
        {
            RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.helpers.js"));
        }

        private void LoadCryptoJS()
        {
            RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.crypto.js"));
        }

        private void ExposeCryptoHelpers()
        {
            _context.GlobalObject.Binding.SetFunction("__randomNextInt__", _randomGenerator.NextInt);
            _context.GlobalObject.Binding.SetFunction<JSValue, JSValue, JSValue, JSValue, string>("__getHmacDigest__",
                HmacHelper.GetHmacDigest);
        }

        private void RunScript(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                _context.RunScript(reader.ReadToEnd());
            }
        }

        private void ExposeAElfOption()
        {
            _context.RunScript("var _config = new Object()");
            var config = _context.GlobalObject.ReadProperty<JSValue>("_config");
            config.WriteProperty<string>("endpoint", _option.Endpoint);
        }

        private static JavaScriptValue ToJSMethod(IServiceNode node, Action<IEnumerable<JavaScriptValue>> a)
        {
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

        private void ExposeHttpRequestorToContext()
        {
            _context.ServiceNode.GetService<IJSValueConverterService>()
                .RegisterProxyConverter<HttpRequestor>( //register the object converter
                    (binding, instance, serviceNode) =>
                    {
                        binding.SetFunction<JSValue, JSValue>("send", instance.Send);
                    });
            try
            {
                RunScript("_requestor = null;");
                _requestor = new HttpRequestor(_option.Endpoint, _context);
                _context.GlobalObject.WriteProperty("_requestor", _requestor);
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        public void RunScript(string jsContent)
        {
            _context.RunScript(jsContent);
        }

        public JSValue Evaluate(string script)
        {
            return new JSValue(_context.ServiceNode, _context.Eval(script));
        }

        public void Execute(string script)
        {
            try
            {
                var res = _context.Eval(script);
                _prettyPrint.PrintValue(res);
                this.AssignToUnderscore(res);
            }
            catch (JavaScriptScriptException e)
            {
                _prettyPrint.PrintError(e.Message);
            }
            catch (InvalidOperationException e)
            {
                Console.Error.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }
    }
}