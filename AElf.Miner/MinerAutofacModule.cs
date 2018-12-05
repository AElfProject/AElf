using AElf.Kernel.Types.Transaction;
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
            
            builder.RegisterType(typeof(Miner.Miner)).As<IMiner>().SingleInstance();
            builder.RegisterType<TxSignatureVerifier>().As<ITxSignatureVerifier>();
            builder.RegisterType<TxRefBlockValidator>().As<ITxRefBlockValidator>();
//            builder.RegisterType<NewTxHub>().SingleInstance();
            builder.RegisterType<TxHub>().As<ITxHub>().As<TxHub>().SingleInstance();
        }
    }
}