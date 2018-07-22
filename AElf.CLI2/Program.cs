using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using Autofac;
using CommandLine;

namespace AElf.CLI2
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<AccountNewOption>(args)
                .MapResult(
                    (AccountNewOption opt) =>
                    {
                        var cmd = IoCContainerBuilder.Build(opt).Resolve<ICommand>();
                        cmd.Execute();
                        return 0;
                    },
                    errs => 1);
        }
    }
}