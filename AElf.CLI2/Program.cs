using System;
using System.Reflection;
using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using Autofac;
using CommandLine;

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
            return Parser.Default.ParseArguments<AccountNewOption, AnotherVerb>(args)
                .MapResult(
                    (AccountNewOption opt) =>
                    {
                        var cmd = IoCContainerBuilder.Build(opt, new BridgeJSProvider()).Resolve<ICommand>();
                        cmd.Execute();
                        return 0;
                    },
                    (AnotherVerb opt) => 0,
                    errs => 1);
        }
    }
}