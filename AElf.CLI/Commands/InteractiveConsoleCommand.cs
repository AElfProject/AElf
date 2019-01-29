using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AElf.CLI.JS;
using Alba.CsConsoleFormat;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET;
using ChakraCore.NET.API;
using CommandLine;
using Console = System.Console;

namespace AElf.CLI.Commands
{
    [Verb("console", HelpText = "Open the interactive console.")]
    public class InteractiveConsoleOption : BaseOption
    {
    }


    public class InteractiveConsoleCommand : Command
    {
        private readonly InteractiveConsoleOption _option;

        public InteractiveConsoleCommand(InteractiveConsoleOption option) : base(option)
        {
            _option = option;
            // TODO: Test endpoint ok
            if (string.IsNullOrWhiteSpace(_option.Endpoint))
            {
                Colors.WriteLine("Endpoint is not set, some functionalities cannot work.".DarkRed());
            }
        }

        private HashSet<string> _jsBuiltins = new HashSet<string>
        {
            "Infinity", "NaN", "undefined", "null", "eval", "uneval", "isFinite", "isNaN", "parseFloat", "parseInt",
            "decodeURI", "decodeURIComponent", "encodeURI", "encodeURIComponent", "escape", "unescape", "Object",
            "Function", "Boolean", "Symbol", "Error", "EvalError", "InternalError", "RangeError", "ReferenceError",
            "SyntaxError", "TypeError", "URIError", "Number", "Math", "Date", "String", "RegExp", "Array", "Int8Array",
            "Uint8Array", "Uint8ClampedArray", "Int16Array", "Uint16Array", "Int32Array", "Uint32Array", "Float32Array",
            "Float64Array", "Map", "Set", "WeakMap", "WeakSet", "ArrayBuffer", "SharedArrayBuffer ", "Atomics ",
            "DataView", "JSON", "Promise", "Generator", "GeneratorFunction", "AsyncFunction ", "ReflectionSection",
            "Reflect", "Proxy", "Intl", "WebAssemblySection", "WebAssembly", "arguments"
        };

        private HashSet<string> _jsObjectProperties = new HashSet<string>()
        {
            "constructor", "__defineGetter__", "__defineSetter__", "hasOwnProperty", "__lookupGetter__",
            "__lookupSetter__", "isPrototypeOf", "propertyIsEnumerable", "toString", "valueOf", "__proto__",
            "toLocaleString"
        };

        private HashSet<string> _otherIgnorable = new HashSet<string>()
        {
            "RequireNative", "XMLHttpRequest"
        };

        private readonly Func<string, bool> _isSystemProvided = (s) => new Regex(@"__[\w\d]+__").IsMatch(s);

        private void PrintInjectedObjects(bool filtered = true)
        {
            var props = _engine.GetObjectPropertyNames(_engine.GlobalObject.ReferenceValue)
                .Where(x => !_jsBuiltins.Contains(x) && !_jsObjectProperties.Contains(x) &&
                            !_otherIgnorable.Contains(x) && !_isSystemProvided(x) || !filtered
                ).OrderBy(x => x).ToList();
            if (props.Count == 0)
            {
                return;
            }

            var cellWidth = props.Max(x => x.Length) + 1;
            var maxCellPerLine = Console.WindowWidth / cellWidth;
            var lines = props
                .Select((x, i) => (x, i))
                .GroupBy(x => x.Item2 / maxCellPerLine)
                .OrderBy(x => x.Key)
                .Select(x => string.Join("", x.Select(y => y.Item1.PadRight(cellWidth))));

            foreach (var l in lines)
            {
                Colors.WriteLine(l);
            }
        }

        public override void Execute()
        {
            InitChain();
            Colors.WriteLine(
                new Span("Welcome to aelf interactive console. Type "),
                "exit".Cyan(), new Span(" to terminate the program. Type "),
                "dir".Cyan(), new Span(" to list objects.")
            );
            Colors.WriteLine("The following objects exist:");
            PrintInjectedObjects();

            ReadLine.AutoCompletionHandler = new CompleteHandler(_engine);
            List<string> lines = new List<string>();
            while (true)
            {
                var line = "";
                if (lines.Count == 0)
                {
                    line = ReadLine.Read("> ");
                }
                else
                {
                    line = ReadLine.Read("... ");
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lines.Add(line);
                var indents = CountIndents(lines);
                if (indents > 0)
                {
                    continue;
                }

                var input = string.Join("\n", lines);
                ReadLine.AddHistory(input);

                // stop the repl if "quit", "Quit", "QuiT", ... is encountered
                if (input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                    input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (input.Trim().Equals("dir"))
                {
                    PrintInjectedObjects();
                    continue;
                }

                if (input.Trim().Equals("dir all"))
                {
                    PrintInjectedObjects(false);
                    continue;
                }

                _engine.Execute(input);
                lines = new List<string>();
            }
        }

        private int CountIndents(IEnumerable<string> lines)
        {
            var indents = 0;
            var inString = false;
            var strOpenChar = ' '; // keep track of the string open char to allow var str = "I'm ....";
            var charEscaped = false; // keep track if the previous char was the '\' char, allow var str = "abc\"def";
            foreach (var line in lines)
            {
                foreach (var c in line)
                {
                    switch (c)
                    {
                        case '\\':
                            if (!charEscaped && inString)
                            {
                                charEscaped = true;
                            }

                            break;
                        case '\'':
                        case '"':
                            if (inString && !charEscaped && strOpenChar == c)
                            {
                                inString = false;
                            }
                            else if (!inString && !charEscaped)
                            {
                                inString = true;
                                strOpenChar = c;
                            }

                            charEscaped = false;
                            break;
                        case '{':
                        case '(':
                            if (!inString)
                            {
                                indents++;
                            }

                            charEscaped = false;
                            break;
                        case '}':
                        case ')':
                            if (!inString)
                            {
                                indents--;
                            }

                            charEscaped = false;
                            break;
                        default:
                            charEscaped = false;
                            break;
                    }
                }
            }

            return indents;
        }
    }

    class CompleteHandler : IAutoCompleteHandler
    {
        private readonly IJSEngine _engine;

        public CompleteHandler(IJSEngine engine)
        {
            _engine = engine;
        }

        public string[] GetSuggestions(string line, int index)
        {
            if (index > 0)
            {
                line = line.Substring(index, line.Length - index);
            }

            var parts = line.Split(".");
            var objRef = "this";
            var prefix = line;
            if (parts.Length > 1)
            {
                objRef = string.Join(".", parts.SkipLast(1));
                prefix = parts.Last();
            }

            JSValue obj = null;
            try
            {
                obj = _engine.Evaluate(objRef);
            }
            catch (Exception)
            {
                return null;
            }

            if (obj.ReferenceValue.ValueType == JavaScriptValueType.Undefined)
            {
                return null;
            }

            var props = _engine.GetObjectPropertyNames(obj.ReferenceValue);
            var results = props.Where(x => x.StartsWith(prefix)).Select(x => objRef == "this" ? x : $"{objRef}.{x}")
                .ToList();
            // Function (append parethesis) or Object (append dot)
            if (results.Count == 1 && results[0] == line)
            {
                var self = _engine.Evaluate(line);
                if (self.ReferenceValue.ValueType == JavaScriptValueType.Function)
                {
                    results[0] = $"{results[0]}(";
                }

                if (self.ReferenceValue.ValueType == JavaScriptValueType.Object)
                {
                    results[0] = $"{results[0]}.";
                }
            }

            return results.ToArray();
        }

        public char[] Separators { get; set; } = {' ', '=', '{', '(', ';'};
    }
}