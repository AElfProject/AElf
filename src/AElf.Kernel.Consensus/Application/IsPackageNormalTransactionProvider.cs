using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application
{
    public class IsPackageNormalTransactionProvider : IIsPackageNormalTransactionProvider, ISingletonDependency
    {
        public bool IsPackage { get; set; } = true;
    }
}