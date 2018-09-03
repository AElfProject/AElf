using AElf.Miner.Rpc.Client;
using AElf.Miner.Rpc.Server;
using Autofac;

namespace AElf.Miner.Rpc
{
    public class MinerRpcAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MinerClientGenerator>().As<MinerClientGenerator>().SingleInstance();
            builder.RegisterType<HeaderInfoServerImpl>().As<HeaderInfoServerImpl>().SingleInstance();
            builder.RegisterType<MinerServer>().As<MinerServer>().SingleInstance();
        }
    }
}