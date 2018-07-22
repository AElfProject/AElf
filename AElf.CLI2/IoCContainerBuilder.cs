using AElf.CLI2.Commands;
using AElf.CLI2.JS;
using AElf.Kernel.Modules.AutofacModule;
using Autofac;

namespace AElf.CLI2
{
    public static class IoCContainerBuilder
    {
        public static IContainer Build(BaseOption option)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new LoggerModule("aelf-cli"));
            builder.RegisterModule(new JSModule());
            var cmd = new CMDModule(option);
            builder.RegisterModule(cmd);
            var container = builder.Build();
            return container;
        }
    }
}