using System;
using System.Reflection;
using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using Autofac;
using CommandLine;
using Console = System.Console;

namespace AElf.CLI2
{
    class Program
    {
        [Verb("another", HelpText = "...")]
        class AnotherVerb : BaseOption
        {
        }

        static int Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<CreateOption, InteractiveOption, DeployOption, GetAbiOption, SendTransactionOption,
                    GetTxResultOption, AnotherVerb>(args)
                .MapResult(
                    (CreateOption opt) =>
                    {
                        using (var cmd = new CreateCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (InteractiveOption opt) =>
                    {
                        using (var cmd = new InteractiveCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (DeployOption opt) =>
                    {
                        using (var cmd = new DeployCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetAbiOption opt) =>
                    {
                        using (var cmd = new GetAbiCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (SendTransactionOption opt) =>
                    {
                        using (var cmd = new SendTransactionCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetTxResultOption opt) =>
                    {
                        using (var cmd = new GetTxResultCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (AnotherVerb opt) => 0,
                    errs => 1);
        }
    }
}