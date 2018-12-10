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
        static int Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<CreateOption, InteractiveOption, DeployOption, GetAbiOption, SendTransactionOption,
                    GetTxResultOption, GetBlockHeightOption, GetBlockInfoOption, GetMerkelPathOption>(args)
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
                    (GetBlockHeightOption opt) =>
                    {
                        using (var cmd = new GetBlockHeightCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetBlockInfoOption opt) =>
                    {
                        using (var cmd = new GetBlockInfoCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetMerkelPathOption opt) =>
                    {
                        using (var cmd = new GetMerkelPathCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    errs => 1);
        }
    }
}