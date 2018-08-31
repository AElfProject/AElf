using AElf.Common.Module;
using AElf.Configuration.Config.RPC;
using Autofac;

namespace AElf.RPC
{
    public class RpcAElfModule:IAElfModule
    {
        public void Init(ContainerBuilder builder)
        {
            builder.RegisterModule(new RpcAutofacModule());
        }

        public void Run(ILifetimeScope scope)
        {
            var rpc = scope.Resolve<IRpcServer>();
            rpc.Init(scope, RpcConfig.Instance.Host, RpcConfig.Instance.Port);
        }
    }
}