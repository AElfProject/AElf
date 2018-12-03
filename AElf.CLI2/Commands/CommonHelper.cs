using AElf.CLI2.JS;
using Autofac;

namespace AElf.CLI2.Commands
{
    public static class CommonHelper
    {
        public static IJSEngine GetJSEngine(this ICommand command, CreateOption createOption)
        {
            return IoCContainerBuilder.Build(createOption).Resolve<IJSEngine>();
        }
    }
}