using AElf.CLI.Commands;
using AElf.CLI.JS;
using AElf.CLI.JS.IO;
using Autofac;
using Autofac.Core;

namespace AElf.CLI
{
    public static class IoCContainerBuilder
    {
        public static IContainer Build(BaseOption option)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new JSModule());
            var cmd = new CmdModule(option);
            builder.RegisterModule(cmd);
            var container = builder.Build();
            return container;
        }
    }
}