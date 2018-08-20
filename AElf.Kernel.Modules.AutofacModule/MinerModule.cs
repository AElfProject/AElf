using Autofac;
using AElf.ChainController;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MinerModule : Module
    {
        public IMinerConfig MinerConf { get; }

        public MinerModule(IMinerConfig minerConfig)
        {
            MinerConf = minerConfig;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (MinerConf != null)
            {
                builder.RegisterInstance(MinerConf).As<IMinerConfig>();
            }
            else
            {
                builder.RegisterInstance(MinerConfig.Default).As<IMinerConfig>();
            }
            
//            builder.RegisterType(typeof(ParallelTransactionExecutingService));//.As<IParallelTransactionExecutingService>();
            builder.RegisterType(typeof(Miner)).As<IMiner>();
//            builder.RegisterType(typeof(ConcurrencyExecutingService)).As<IExecutingService>().SingleInstance();
        }
    }
}    