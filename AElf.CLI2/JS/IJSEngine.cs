using ChakraCore.NET.API;

namespace AElf.CLI2.JS
{
    public interface IJSObject
    {
        IJSObject Get(string name);
        TResult Invoke<T, TResult>(string methodName, T arg);
        TResult Invoke<TResult>(string methodName);
    }
    public interface IJSEngine: IJSObject
    {
    }
}