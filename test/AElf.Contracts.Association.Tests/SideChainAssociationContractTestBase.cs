using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Contracts.Association
{
    public class
        SideChainAssociationContractTestBase : AssociationContractTestBase<
            AssociationContractTestAElfModuleWithSpecificChainId>
    {
        internal async Task<Address> CreateOrganization(CreateOrganizationInput input)
        {
            DeployContracts();
            var transactionResult = await AssociationContractStub.CreateOrganization.SendAsync(input);
            var organizationAddress = transactionResult.Output;
            return organizationAddress;
        }
    }
}