using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AElf.Common.Attributes;
using AElf.Kernel.Consensus;
using ChakraCore.NET.API;
using ChakraCore.NET.Debug;
using NLog;
using NServiceKit.Common.Extensions;

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

        public void Log(IEnumerable<JavaScriptValue> args)
        {
            Log(LogLevel.Info, args);
        }

        public void Debug(IEnumerable<JavaScriptValue> args)
        {
            Log(LogLevel.Debug, args);
        }

        public void Warn(IEnumerable<JavaScriptValue> args)
        {
            Log(LogLevel.Warn, args);
        }

        public void Error(IEnumerable<JavaScriptValue> args)
        {
            Log(LogLevel.Error, args);
        }

        private void Log(LogLevel level, IEnumerable<JavaScriptValue> args)
        {
            _logger.Log(level, string.Join(" ", args.Select(JSValueToString)));
        }

        private string JSValueToString(JavaScriptValue val)
        {
            switch (val.ValueType)
            {
                case JavaScriptValueType.Boolean:
                    return val.ToBoolean() ? "true" : "false";
                case JavaScriptValueType.Number:
                    try
                    {
                        return val.ToInt32().ToString();
                    }
                    catch (JavaScriptException)
                    {
                        return val.ToDouble().ToString(CultureInfo.InvariantCulture);
                    }
                case JavaScriptValueType.Error:
                    throw new Exception("Error in js");
                default:
                    _logger.Debug(val.ValueType);
                    return val.ValueType.ToString();
            }
        }
    }
}