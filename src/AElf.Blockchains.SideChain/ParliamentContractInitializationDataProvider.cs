using AElf.CrossChain.Application;
using AElf.GovernmentSystem;
using AElf.Kernel.Proposal;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public class ParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public ParliamentContractInitializationDataProvider(ISideChainInitializationDataProvider sideChainInitializationDataProvider)
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