using AElf.Common.Module;
using AElf.Configuration.Config.RPC;
using Autofac;

namespace AElf.Net.Rpc
{
    public class NetRpcAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new NetRpcAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
        }
    }
}