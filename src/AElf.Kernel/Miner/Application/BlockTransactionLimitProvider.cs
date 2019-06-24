using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Miner.Application
{
    public class BlockTransactionLimitProvider : IBlockTransactionLimitProvider, ISingletonDependency
    {
        public int Limit { get; set; } = 0;
    }
}