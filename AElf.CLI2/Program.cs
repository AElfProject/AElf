using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.CLI2.Commands;
using AElf.CLI2.Commands.CrossChain;
using AElf.CLI2.Commands.Proposal;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using Autofac;
using CommandLine;
using Console = System.Console;

namespace AElf.CLI2
{
    class Program
    {
        static int Main(string[] args)
        {
            return ParseArguments(args, CmdModule.Commands.Keys.ToArray());
        }

        private static int ParseArguments(IEnumerable<string> args, params Type[] types)
        {
            var parsedResult = Parser.Default.ParseArguments(args, types);
            Parsed<object> parsed = parsedResult as Parsed<object>;
            if (parsed == null)
                return 1;
            Type optionType = null;
            var opt = parsed.Value;
            Type cmdType = CmdModule.Commands.First(cmd =>
            {
                if (opt.GetType() != cmd.Key)
                    return false;
                optionType = cmd.Key;
                return opt.GetType() == cmd.Key;
            }).Value;
            if (optionType == null)
                return 1;
            var command = cmdType.GetConstructor(new[] {optionType}).Invoke(new[] {opt});
            cmdType.GetMethod("Execute").Invoke(command, new Object[0]);
            return 0;
        }

    }
}