using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Miner.Rpc.Server;
using Autofac;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Miner.Rpc
{
    public class MinerRpcAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SideChainBlockInfoRpcServerImpl>().SingleInstance().OnActivated(impl =>
            {
                impl.Instance.Init(Hash.Loads(NodeConfig.Instance.ChainId));
            });
            builder.RegisterType<ParentChainBlockInfoRpcServerImpl>().SingleInstance().OnActivated(impl =>
            {
                impl.Instance.Init(Hash.Loads(NodeConfig.Instance.ChainId));
            });
        }
    }
}