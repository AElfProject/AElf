using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AElf.CLI.Commands;
using AElf.CLI.JS.Crypto;
using AElf.CLI.JS.IO;
using Alba.CsConsoleFormat;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET;
using ChakraCore.NET.API;
using ChakraCore.NET.Debug;
using ChakraCore.NET.Hosting;
using Console = System.Console;

namespace AElf.CLI.JS
{
    public class PrettyPrint
    {
        private const int IndentSize = 2;

        private const int MaxLevel = 3;

        // These fields won't be printed
        private readonly HashSet<string> _boringKeys = new HashSet<string>()
        {
            "valueOf",
            "toString",
            "toLocaleString",
            "hasOwnProperty",
            "isPrototypeOf",
            "propertyIsEnumerable",
            "constructor"
        };

        private readonly IJSEngine _engine;

        public PrettyPrint(IJSEngine engine)
        {
            _engine = engine;
        }

        public void PrintValue(JavaScriptValue value, int level = 0)
        {
            // ReSharper disable once CoVariantArrayConversion
            var doc = new Document(GetValueSpans(value, level).ToArray());
            ConsoleRenderer.RenderDocument(doc);
        }

        public void PrintError(string error)
        {
            Colors.WriteLine(error.Replace("Script threw an exception. ", "").DarkRed());
        }

        private string GetFuncRep(JavaScriptValue value)
        {
            try
            {
                var func = _engine.GetFunctionToString(value);
                return func.Split("{")[0].Trim(' ', '\t', '\n').Replace(" (", "(");
            }
            catch (Exception)
            {
                return "function()";
            }
        }

        private IEnumerable<Span> GetArraySpans(JavaScriptValue obj, int level)
        {
            int len = _engine.GetArraySize(obj);
            if (len == 0)
            {
                return new[] {new Span("[]")};
            }

            if (level > MaxLevel)
            {
                return new[] {new Span("[...]")};
            }

            List<Span> spans = new List<Span>();
            spans.Add(new Span("["));
            for (int i = 0; i < len; i++)
            {
                spans.AddRange(GetValueSpans(_engine.GetObjectProperty(obj, i.ToString()), level + 1));
                if (i < len - 1)
                {
                    spans.Add(new Span(", "));
                }
            }

            spans.Add(new Span("]"));
            return spans;
        }

        private IEnumerable<Span> GetObjectSpans(JavaScriptValue obj, int level, bool inArray)
        {
            var props = _engine.GetObjectPropertyNames(obj);
            var vals = new Dictionary<string, JavaScriptValue>();
            var functions = new Dictionary<string, JavaScriptValue>();
            foreach (var p in props.Where(x => !_boringKeys.Contains(x) && !x.StartsWith("_")))
            {
                var pVal = _engine.GetObjectProperty(obj, p);
                switch (pVal.ValueType)
                {
                    case JavaScriptValueType.Function:
                        functions[p] = pVal;
                        break;
                    default:
                        vals[p] = pVal;
                        break;
                }
            }

            if (level > MaxLevel)
            {
                return new[] {new Span("{...}")};
            }

            List<Span> spans = new List<Span>();
            spans.Add(new Span("{\n"));
            Action<Dictionary<string, JavaScriptValue>, bool> addToSpans = (dict, shouldContinue) =>
            {
                foreach (var (k, i) in dict.Keys.OrderBy(x => x).Select((k, i) => (k, i)))
                {
                    spans.Add(GetPadding(level + 1));
                    spans.Add(new Span($"{k}: "));
                    spans.AddRange(GetValueSpans(dict[k], level + 1, inArray));
                    if (i < dict.Count - (shouldContinue ? 0 : 1))
                    {
                        spans.Add(new Span(","));
                    }

                    spans.Add(new Span("\n"));
                }
            };
            addToSpans(vals, functions.Count > 0);
            addToSpans(functions, false);
            spans.Add(GetPadding(level - (inArray ? 1 : 0)));
            spans.Add(new Span("}"));
            return spans;
        }

        private Span GetPadding(int level)
        {
            return new Span("".PadLeft(IndentSize * level));
        }

        private IEnumerable<Span> GetValueSpans(JavaScriptValue value, int level = 0, bool inArray = false)
        {
            switch (value.ValueType)
            {
                case JavaScriptValueType.Null:
                    return new Span[] {new Span("null")};
                case JavaScriptValueType.Undefined:
                    return new Span[] {new Span("undefined")};
                case JavaScriptValueType.String:
                    return new Span[] {$"\"{value.ToString().Replace("\"", "\\\"")}\"".DarkGreen()};
                case JavaScriptValueType.Boolean:
                    return new Span[] {new Span(value.ToBoolean() ? "true" : "false")};
                case JavaScriptValueType.Number:
                    try
                    {
                        return new Span[] {value.ToInt32().ToString().Red()};
                    }
                    catch (JavaScriptException)
                    {
                        return new Span[] {value.ToDouble().ToString(CultureInfo.InvariantCulture).Red()};
                    }
                case JavaScriptValueType.Function:
                    return new Span[] {GetFuncRep(value).Magenta()};
                case JavaScriptValueType.Error:
                    return new Span[] {value.ToJsonString().DarkRed()};
                case JavaScriptValueType.Object:
                    return GetObjectSpans(value, level, inArray);
                case JavaScriptValueType.Array:
                    return GetArraySpans(value, level);
                default:
                    return new Span[] {new Span(value.ValueType.ToString()),};
            }
        }
    }
}