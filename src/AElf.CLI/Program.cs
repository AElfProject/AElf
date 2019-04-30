using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AElf.CLI.Commands;
using AElf.CLI.Commands.CrossChain;
using AElf.CLI.Commands.Proposal;
using AElf.CLI.JS;
using AElf.CLI.JS.IO;
using CommandLine;
using Console = System.Console;

namespace AElf.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            var parsedResult = Parser.Default.ParseArguments(args, CmdModule.Commands.Keys.ToArray());
            if (!(parsedResult is Parsed<object> parsed))
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
            cmdType.GetMethod("Execute").Invoke(command, new object[0]);
            return 0;
        }
    }
}