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
            builder.RegisterType<MinerClientManager>().As<MinerClientManager>().SingleInstance();
            builder.RegisterType<HeaderInfoServerImpl>().As<HeaderInfoServerImpl>().SingleInstance();
            builder.RegisterType<MinerServer>().As<MinerServer>().SingleInstance();
        }

        public void Run(ILifetimeScope scope)
        {
            /*var evListener = scope.Resolve<MinerClientManager>();
            MessageHub.Instance.Subscribe<IBlock>(async (t) =>
            {
                await evListener.(t);
            });*/
        }
    }
}