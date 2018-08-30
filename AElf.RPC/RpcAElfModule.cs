using AElf.Common.Module;
using AElf.Configuration.Config.RPC;
using Autofac;

namespace AElf.RPC
{
    public class RpcAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
            
        }

        public void Run(ILifetimeScope scope)
        {
            var rpc = scope.Resolve<IRpcServer>();
            rpc.Init(scope, RpcConfig.Instance.Host, RpcConfig.Instance.Port);
        }
    }
}