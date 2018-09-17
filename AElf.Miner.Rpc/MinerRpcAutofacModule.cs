using AElf.Common.Application;
using AElf.Miner.Rpc.Server;
using Autofac;

namespace AElf.Miner.Rpc
{
    public class MinerRpcAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SideChainBlockInfoRpcServerImpl>().As<SideChainBlockInfoRpcServerImpl>()
                .SingleInstance();
            builder.RegisterType<ParentChainBlockInfoRpcServerImpl>().As<ParentChainBlockInfoRpcServerImpl>()
                .SingleInstance();
            builder.RegisterType<ServerManager>().SingleInstance().OnActivated(mc =>
                {
                    mc.Instance.Init(ApplicationHelpers.GetDefaultDataDir());
                }
            );
        }
    }
}