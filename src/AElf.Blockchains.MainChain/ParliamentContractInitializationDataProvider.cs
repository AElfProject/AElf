using AElf.GovernmentSystem;
using AElf.Kernel.Proposal;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.MainChain
{
    public class ParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider,
        ITransientDependency
    {
        public ParliamentContractInitializationData GetContractInitializationData()
        {
            return new ParliamentContractInitializationData();
        }
    }
}