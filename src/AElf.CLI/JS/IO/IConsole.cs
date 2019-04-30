using System.Collections.Generic;
using ChakraCore.NET;
using ChakraCore.NET.API;

namespace AElf.CLI.JS.IO
{
    public interface IConsole
    {
        void Log(IEnumerable<JavaScriptValue> args);
        void Debug(IEnumerable<JavaScriptValue> args);
        void Warn(IEnumerable<JavaScriptValue> args);

        void Error(IEnumerable<JavaScriptValue> args);
    }
}