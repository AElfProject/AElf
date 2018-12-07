using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AElf.Common.Attributes;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using NLog;

namespace AElf.CLI2.JS.IO
{
    public class Console : IConsole
    {
        private readonly ILogger _logger = LogManager.GetLogger("js.console");

        public void Log(IEnumerable<JavaScriptValue> args)
        {
            Colors.WriteLine(
                string.Join(" ", args.Select(JSValueToString))
            );
        }

        public void Debug(IEnumerable<JavaScriptValue> args)
        {
            Colors.WriteLine(
                string.Join(" ", args.Select(JSValueToString)).Magenta()
            );
        }

        public void Warn(IEnumerable<JavaScriptValue> args)
        {
            Colors.WriteLine(
                string.Join(" ", args.Select(JSValueToString)).Yellow()
            );
        }

        public void Error(IEnumerable<JavaScriptValue> args)
        {
            Colors.WriteLine(
                string.Join(" ", args.Select(JSValueToString)).Red()
            );
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