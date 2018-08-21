using Autofac;
using AElf.Execution;
using AElf.Miner.Miner;

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
            
            builder.RegisterType(typeof(Miner.Miner.Miner)).As<IMiner>();
            builder.RegisterType(typeof(BlockExecutor)).As<IBlockExecutor>();
        }
    }
}    