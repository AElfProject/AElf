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

    public interface IJSEngine : IJSObject
    {
        void RunScript(string jsContent);
    }
}