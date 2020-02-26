using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataNameProvider : IBlockExtraDataNameProvider, ISingletonDependency
    {
        public static string Name = "CrossChain";
        public string ExtraDataName => Name;
    }
}