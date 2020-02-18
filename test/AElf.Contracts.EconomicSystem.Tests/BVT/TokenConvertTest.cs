using System.Threading.Tasks;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task TransferAuthorizationForTokenConvert_Test()
        {
            var newParliament = new Parliament.CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new Acs3.ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var createNewParliament =
                (await ParliamentContractStub.CreateOrganization.SendAsync(newParliament)).TransactionResult;
            createNewParliament.Status.ShouldBe(TransactionResultStatus.Mined);
            var calculatedNewParliamentAddress = await ParliamentContractStub.CalculateOrganizationAddress.CallAsync(newParliament);
            await ExecuteProposalTransaction(Tester, TokenConverterContractAddress, nameof(TokenConverterContractContainer.TokenConverterContractStub.ChangeConnectorController), calculatedNewParliamentAddress);
            var controller = await TokenConverterContractStub.GetControllerForManageConnector.CallAsync(new Empty());
            controller.ShouldBe(calculatedNewParliamentAddress);
        }
    }
}