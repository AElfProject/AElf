using ChakraCore.NET;
using ChakraCore.NET.API;

namespace AElf.CLI2.JS.IO
{
    public interface IConsole
    {
        void Log(JavaScriptValue args);
        void Debug(JavaScriptValue args);
        void Warn(JavaScriptValue args);
        void Error(JavaScriptValue args);
    }
}