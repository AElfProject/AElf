using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using Org.BouncyCastle.Bcpg;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthTests : AssociationAuthContractTestBase
    {
        public AssociationAuthTests() {
            DeployContracts();
        }
        [Fact]
        public async Task Create_Organization()
        {
            var input =  new CreateOrganizationInput
            {
                Reviewers = { },
                ReleaseThreshold = 2,
                ProposerThreshold = 3
            };
            var transactionResult =
                await AssociationAuthContractStub.CreateOrganization.SendAsync(input);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var getOrganization = await AssociationAuthContractStub.GetOrganization.CallAsync(transactionResult.Output);
            getOrganization.OrganizationAddress.ShouldBe(transactionResult.Output);
            getOrganization.Reviewers.ShouldBe(input.Reviewers);
            getOrganization.ProposerThreshold.ShouldBe(3);
            getOrganization.ReleaseThreshold.ShouldBe(2);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(Hash.FromMessage(AssociationAuthContractAddress), Hash.FromMessage(input)));
        }

        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var transactionResult =
                await AssociationAuthContractStub.GetOrganization.SendAsync(Address.Generate());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }
    }
}