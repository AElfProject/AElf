using AElf.Kernel;
using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using Autofac;
using Easy.MessageHub;

namespace AElf.Miner.Rpc
{
    public class MinerRpcAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ClientManager>().As<ClientManager>().SingleInstance();
            builder.RegisterType<SideChainBlockInfoRpcServerImpl>().As<SideChainBlockInfoRpcServerImpl>().SingleInstance();
        }
    }
}