using AElf.Miner.Miner;
using AElf.Miner.TxMemPool;
using Autofac;

namespace AElf.Miner
{
    public class MinerAutofacModule : Module
    {
        public IMinerConfig MinerConf { get; }

        public MinerAutofacModule(IMinerConfig minerConfig)
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
            
            builder.RegisterType(typeof(Miner.Miner)).As<IMiner>();
            builder.RegisterType<NewTxHub>().SingleInstance();
            builder.RegisterType<TxPool>().As<ITxPool>().SingleInstance();
            builder.RegisterType<TxValidator>().As<ITxValidator>().SingleInstance();
        }
    }
}