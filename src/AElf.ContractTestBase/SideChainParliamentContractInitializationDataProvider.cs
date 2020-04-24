using AElf.CrossChain.Application;
using AElf.GovernmentSystem;
using Volo.Abp.Threading;

namespace AElf.ContractTestBase
{
    public class SideChainParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public SideChainParliamentContractInitializationDataProvider(ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
        }

        public ParliamentContractInitializationData GetContractInitializationData()
        {
            var sideChainInitializationData =
                AsyncHelper.RunSync(_sideChainInitializationDataProvider.GetChainInitializationDataAsync);

            return new ParliamentContractInitializationData
            {
                PrivilegedProposer = sideChainInitializationData.Creator,
                ProposerAuthorityRequired = sideChainInitializationData.ChainCreatorPrivilegePreserved
            };
        }
    }
}