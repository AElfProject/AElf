using Acs0;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateReferendumInitializationCallList()
        {
            var referendumInitializationCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            return referendumInitializationCallList;
        }
    }
}