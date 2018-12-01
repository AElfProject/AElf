using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using Autofac;
using Autofac.Core;

namespace AElf.CLI2
{
    public static class IoCContainerBuilder
    {
        public static IContainer Build(BaseOption option)
        {
//            if (loggerModule == null)
//            {
//                loggerModule = new LoggerModule("aelf-cli");
//            }
            var builder = new ContainerBuilder();
//            builder.RegisterModule(loggerModule);
            builder.RegisterModule(new JSModule());
            var cmd = new CMDModule(option);
            builder.RegisterModule(cmd);
            var container = builder.Build();
            return container;
        }
    }
}