using System.Collections.Generic;
using System.Linq;
using AElf.Common.Attributes;
using ChakraCore.NET.API;
using NLog;

namespace AElf.CLI2.JS.IO
{
    [LoggerName("js.console")]
    public class Console : IConsole
    {
        private readonly ILogger _logger;

        public Console(ILogger logger)
        {
            _logger = logger;
        }

        public void Log(JavaScriptValue args)
        {
            Log(LogLevel.Info, args);
        }

        public void Debug(JavaScriptValue args)
        {
            Log(LogLevel.Debug, args);
        }

        public void Warn(JavaScriptValue args)
        {
            Log(LogLevel.Warn, args);
        }

        public void Error(JavaScriptValue args)
        {
            Log(LogLevel.Error, args);
        }

        private void Log(LogLevel level, JavaScriptValue args)
        {
            _logger.Log(level, args.ToString());
        }
    }
}