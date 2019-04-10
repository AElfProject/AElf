using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AElf.CLI.Commands;
using AElf.CLI.JS.Crypto;
using AElf.CLI.JS.IO;
using AElf.CLI.JS.Net;
using AElf.CLI.Utils;
using ChakraCore.NET;
using ChakraCore.NET.API;
using ChakraCore.NET.Debug;
using ChakraCore.NET.Hosting;
using Newtonsoft.Json;
using Console = System.Console;

namespace AElf.CLI.JS
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
        private TimerCallsHelper _timerCallsHelper;
        public string DefaultScriptsPath { get; }

        public IServiceNode ServiceNode => _context.ServiceNode;
        public JSValue GlobalObject => _context.GlobalObject;

        public JSEngine(IConsole console, BaseOption option, IRandomGenerator randomGenerator, IDebugAdapter debugAdapter)
        {
            DefaultScriptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            _console = console;
            var config = new JavaScriptHostingConfig();
            _context = JavaScriptHosting.Default.CreateContext(config);
            _context.RegisterEvalService();
            _prettyPrint = new PrettyPrint(this);
            _timerCallsHelper = new TimerCallsHelper(this);
            _option = option;
            _randomGenerator = randomGenerator;
            ExposeConsoleToContext();
            ExposeCryptoHelpers();
            ExposeAElfOption();
            LoadCryptoJS();
            LoadEncodingJS();
            LoadAelfJs();
            LoadHelpersJs();
            ExposeHttpRequestorToContext();
            ExposeTimerCallsHelper();
            ExposeAccountSaver();
        }

        private void LoadAelfJs()
        {
            RunScript(File.ReadAllText(Path.Combine(DefaultScriptsPath, "aelf.js")));
            RunScript(@"Aelf = require('aelf');");
        }

        private void LoadHelpersJs()
        {
            RunScript(File.ReadAllText(Path.Combine(DefaultScriptsPath, "helpers.js")));
        }

        private void LoadCryptoJS()
        {
            RunScript(File.ReadAllText(Path.Combine(DefaultScriptsPath, "crypto.js")));
        }

        private void LoadEncodingJS()
        {
            RunScript(File.ReadAllText(Path.Combine(DefaultScriptsPath, "encoding.js")));
        }

        private void ExposeCryptoHelpers()
        {
            _context.GlobalObject.Binding.SetFunction("__randomNextInt__", _randomGenerator.NextInt);
            _context.GlobalObject.Binding.SetFunction<JSValue, JSValue, JSValue, JSValue, string>("__getHmacDigest__",
                HmacHelper.GetHmacDigest);
        }

        private void ExposeAccountSaver()
        {
            _context.GlobalObject.Binding.SetMethod<string, string, string, string>("__saveAccount__",
                (address, privKey, pubKey, password) => { Pem.WriteKeyPair(_option.GetPathForAccount(address), privKey, pubKey, password); });
        }

        private void ExposeAElfOption()
        {
            RunScript($"_config = {JsonConvert.SerializeObject(_option)};");
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
                        Action<string, Action<IEnumerable<JavaScriptValue>>> setMethod = (name, fn) => { valServ.WriteProperty<Action<IEnumerable<JavaScriptValue>>>(val, name, fn); };
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
                    (binding, instance, serviceNode) => { binding.SetFunction<JSValue, JSValue>("send", instance.Send); });
            try
            {
                RunScript("_requestor = null;");
                _requestor = new HttpRequestor(_option.Endpoint, _context);
                _context.GlobalObject.WriteProperty("_requestor", _requestor);
                RunScript("aelf = new Aelf(_requestor);");
                RunScript(File.ReadAllText(Path.Combine(DefaultScriptsPath, "requestor.js")));
            }
            catch (Exception)
            {
                //Ignore
            }
        }

        private void ExposeTimerCallsHelper()
        {
            _context.GlobalObject.Binding.SetMethod<JSValue, int, int>("__repeatedCalls__",
                _timerCallsHelper.RepeatedCalls);
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

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}