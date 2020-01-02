using Acs0;
using AElf.Contracts.ReferendumAuth;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateReferendumInitializationCallList()
        {
            var referendumInitializationCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            referendumInitializationCallList.Add(
                nameof(ReferendumAuthContractContainer.ReferendumAuthContractStub.Initialize),
                new Empty());
            return referendumInitializationCallList;
        }
    }
}