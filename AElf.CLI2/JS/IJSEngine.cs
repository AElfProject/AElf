using System.IO;
using ChakraCore.NET;
using ChakraCore.NET.API;

namespace AElf.CLI2.JS
{
    public interface IJSObject
    {
        IJSObject Get(string name);
        TResult Invoke<T, TResult>(string methodName, T arg);
        TResult Invoke<TResult>(string methodName);
        IJSObject InvokeAndGetJSObject(string methodName);

        JavaScriptValue Value { get; }
    }

    public interface IJSEngine
    {
        IServiceNode ServiceNode { get; }
        JSValue GlobalObject { get; }
        void RunScript(Stream stream);
        void RunScript(string jsContent);
        JSValue Evaluate(string script);
        void Execute(string script);
    }
}