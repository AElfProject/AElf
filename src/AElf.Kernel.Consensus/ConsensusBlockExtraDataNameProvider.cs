using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus
{
    public class ConsensusBlockExtraDataNameProvider : IBlockExtraDataNameProvider, ISingletonDependency
    {
        public static string Name = "Consensus";

        public string ExtraDataName => Name;
    }
}