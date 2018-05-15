using AElf.Kernel.TxMemPool;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class TxPoolServiceModule : Module
    {
        public ITxPoolConfig PoolConfig { get; set; }

        public TxPoolServiceModule(ITxPoolConfig poolConfig)
        {
            PoolConfig = poolConfig;
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            if (PoolConfig != null)
                builder.RegisterInstance(PoolConfig).As<ITxPoolConfig>();
            else
                builder.RegisterInstance(TxPoolConfig.Default).As<ITxPoolConfig>();
            
            builder.RegisterType<TxPool>().As<ITxPool>();
            builder.RegisterType<TxPoolService>().As<ITxPoolService>();
        }
    }
}