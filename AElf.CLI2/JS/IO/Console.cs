using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AElf.Common.Attributes;
using ChakraCore.NET.API;
using NLog;

namespace AElf.CLI2.JS.IO
{
    public class Console : IConsole
    {
        private readonly ILogger _logger = LogManager.GetLogger("js.console");

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
                    var keys = val.GetOwnPropertyNames();
                    throw new Exception("Error in js");
                case JavaScriptValueType.String:
                    return val.ToString();
                case JavaScriptValueType.Array:
//                    int length = val.GetProperty(JavaScriptPropertyId.FromString("length")).ToInt32();
//                    return "Array";
                case JavaScriptValueType.Object:
                    return val.ToJsonString();
                default:
                    return val.ValueType.ToString();
            }
        }
    }
}