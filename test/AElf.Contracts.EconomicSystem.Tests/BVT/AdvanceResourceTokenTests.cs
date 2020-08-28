using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        private const long Amount = 100;
        private const string ResourceTokenSymbol = "TRAFFIC";

        private async Task<Address> TokenContract_AdvanceResourceToken_Test()
        {
            var contractAddress = SampleAddress.AddressList[0];
            var developerAddress = BootMinerAddress;

            await TokenConverterContractStub.Buy.SendAsync(new BuyInput
            {
                Symbol = ResourceTokenSymbol,
                Amount = Amount,
            });

            var balanceBeforeAdvancing = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = developerAddress,
                Symbol = ResourceTokenSymbol
            });

            await TokenContractStub.AdvanceResourceToken.SendAsync(new AdvanceResourceTokenInput
            {
                ContractAddress = contractAddress,
                Amount = Amount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            // Check balance of contract address.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = contractAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(Amount);
            }

            // Check balance of developer.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = developerAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(balanceBeforeAdvancing.Balance - Amount);
            }

            return contractAddress;
        }

        [Fact]
        public async Task TokenContract_TakeResourceTokenBack_Test()
        {
            var contractAddress = await TokenContract_AdvanceResourceToken_Test();
            var developerAddress = BootMinerAddress;

            var balanceBeforeTakingBack = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = developerAddress,
                Symbol = ResourceTokenSymbol
            });

            await TokenContractStub.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = contractAddress,
                Amount = Amount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            // Check balance of developer.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = developerAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(balanceBeforeTakingBack.Balance + Amount);
            }

            // Check balance of contract address.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = contractAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(0);
            }
        }

        [Fact]
        public async Task TokenContract_TakeResourceTokenBack_NotAll_Test()
        {
            var contractAddress = await TokenContract_AdvanceResourceToken_Test();
            var developerAddress = BootMinerAddress;
            const long takeBackAmount = Amount / 2;

            var balanceBeforeTakingBack = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = developerAddress,
                Symbol = ResourceTokenSymbol
            });

            await TokenContractStub.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = contractAddress,
                Amount = takeBackAmount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            // Check balance of developer.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = developerAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(balanceBeforeTakingBack.Balance + takeBackAmount);
            }

            // Check balance of contract address.
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = contractAddress,
                    Symbol = ResourceTokenSymbol
                });
                balance.Balance.ShouldBe(Amount - takeBackAmount);
            }
        }

        [Fact]
        public async Task TokenContract_TakeResourceTokenBack_Exceed_Test()
        {
            var contractAddress = await TokenContract_AdvanceResourceToken_Test();
            const long takeBackAmount = Amount * 2;

            var result = await TokenContractStub.TakeResourceTokenBack.SendAsync(new TakeResourceTokenBackInput
            {
                ContractAddress = contractAddress,
                Amount = takeBackAmount,
                ResourceTokenSymbol = ResourceTokenSymbol
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Can't take back that more.");
        }

        [Fact]
        public async Task SetControllerForManageConnector_Test()
        {
            var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });
            var organizationAddress = createOrganizationResult.Output;
            var defaultController =
                await TokenConverterContractStub.GetControllerForManageConnector.CallAsync(new Empty());
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposal = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ToAddress = TokenConverterContractAddress,
                ContractMethodName = nameof(TokenConverterContractStub.ChangeConnectorController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = new AuthorityInfo
                {
                    ContractAddress = defaultController.ContractAddress,
                    OwnerAddress = organizationAddress
                }.ToByteString(),
                OrganizationAddress = defaultOrganization
            });
            var proposalId = proposal.Output;
            await ApproveWithAllMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var newController = await TokenConverterContractStub.GetControllerForManageConnector.CallAsync(new Empty());
            newController.OwnerAddress.ShouldBe(organizationAddress);
            
        }
    }
}