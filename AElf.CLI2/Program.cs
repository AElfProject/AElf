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
            return Parser.Default.ParseArguments<AccountOption, InteractiveOption, AnotherVerb>(args)
                .MapResult(
                    (AccountOption opt) =>
                    {
//                        var sdk = IoCContainerBuilder.Build(opt).Resolve<IAElfSdk>();
//                        sdk.Chain().ConnectChain();
                        new AccountCommand(opt).Execute();
                        return 0;
                    },
                    (InteractiveOption opt) =>
                    {
                        new InteractiveCommand(opt).Execute();
                        return 0;
                    },
                    (AnotherVerb opt) => 0,
                    errs => 1);
        }
    }
}