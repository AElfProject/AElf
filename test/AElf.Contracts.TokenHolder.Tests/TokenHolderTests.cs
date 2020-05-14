using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

// ReSharper disable HeuristicUnreachableCode
namespace AElf.Contracts.TokenHolder
{
    public partial class TokenHolderTests : TokenHolderContractTestBase
    {
        public TokenHolderTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task CheckTokenHolderProfitScheme()
        {
            var schemeIds = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
            {
                Manager = DAppContractAddress
            });
            schemeIds.SchemeIds.Count.ShouldBePositive();
            var schemeId = schemeIds.SchemeIds.First();
            var scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
            scheme.Manager.ShouldBe(DAppContractAddress);
        }

        [Fact]
        public async Task CreateTokenHolderProfitSchemeTest()
        {
            await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
            {
                Symbol = "APP"
            });

            {
                var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);
                tokenHolderProfitScheme.Period.ShouldBe(0);
                tokenHolderProfitScheme.Symbol.ShouldBe("APP");
                tokenHolderProfitScheme.SchemeId.ShouldBeNull();
            }

            await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeManager = Starter,
                Symbol = "ELF",
                Amount = 1
            });

            {
                var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);
                tokenHolderProfitScheme.SchemeId.ShouldNotBeNull();
            }
        }

        [Fact]
        public async Task ContributeProfitsTest()
        {
            await CreateTokenHolderProfitSchemeTest();

            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

            {
                var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
                originScheme.Manager.ShouldBe(Starter);
                originScheme.CurrentPeriod.ShouldBe(1);
                originScheme.TotalShares.ShouldBe(0);

                var generalLedgerBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = originScheme.VirtualAddress,
                    Symbol = "ELF"
                })).Balance;
                generalLedgerBalance.ShouldBe(1);
            }
            
            await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeManager = Starter,
                Symbol = "ELF",
                Amount = 9999
            });

            {
                var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
                var generalLedgerBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = originScheme.VirtualAddress,
                    Symbol = "ELF"
                })).Balance;
                generalLedgerBalance.ShouldBe(10000);
            }
        }

        [Fact]
        public async Task AddBeneficiaryTest()
        {
            await ContributeProfitsTest();
            
            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

            await TokenHolderContractStub.AddBeneficiary.SendAsync(new AddTokenHolderBeneficiaryInput
            {
                Beneficiary = UserAddresses.First(),
                Shares = 1
            });
            
            {
                var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
                originScheme.TotalShares.ShouldBe(1);
            }
        }
        
        [Fact]
        public async Task RemoveBeneficiaryTest()
        {
            await AddBeneficiaryTest();
            
            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

            await TokenHolderContractStub.RemoveBeneficiary.SendAsync(new RemoveTokenHolderBeneficiaryInput
            {
                Beneficiary = UserAddresses.First()
            });
            
            {
                var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
                originScheme.TotalShares.ShouldBe(0);
            }
        }

        [Fact]
        public async Task DistributeProfits_ClaimWithProfitContract()
        {
            await AddBeneficiaryTest();

            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

            await TokenHolderContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeManager = Starter,
                AmountsMap = {{"ELF", 0L}}
            });

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1));
            }

            var userProfitStub =
                GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, UserKeyPairs.First());
            await userProfitStub.ClaimProfits.SendAsync(new Profit.ClaimProfitsInput
            {
                SchemeId = tokenHolderProfitScheme.SchemeId,
            });
            
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1) + 10000);
            }
        }
        
        [Fact]
        public async Task DistributeProfits_ClaimWithTokenHolderContract()
        {
            await AddBeneficiaryTest();

            await TokenHolderContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeManager = Starter,
                AmountsMap = {{"ELF", 0L}}
            });

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1));
            }

            var userTokenHolderStub =
                GetTester<TokenHolderContractImplContainer.TokenHolderContractImplStub>(TokenHolderContractAddress, UserKeyPairs.First());
            await userTokenHolderStub.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeManager = Starter,
            });
            
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1) + 10000);
            }
        }
        
        [Fact]
        public async Task ChangeMethodFeeController_Test()
        {
            var createOrganizationResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);

            var methodFeeController = await TokenHolderContractStub.GetMethodFeeController.CallAsync(new Empty());
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName = nameof(TokenHolderContractStub.ChangeMethodFeeController);
            var proposalId = await CreateProposalAsync(TokenHolderContractAddress,
                methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentContractAddress
                });
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
            releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var newMethodFeeController = await TokenHolderContractStub.GetMethodFeeController.CallAsync(new Empty());
            newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task ChangeMethodFeeController_WithoutAuth_Test()
        {
            var createOrganizationResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);
            var result = await TokenHolderContractStub.ChangeMethodFeeController.SendWithExceptionAsync(new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        private async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
            string methodName, IMessage input)
        {
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = organizationAddress,
                ContractMethodName = methodName,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = contractAddress
            };

            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            var proposalId = createResult.Output;

            return proposalId;
        }

        private async Task ApproveWithMinersAsync(Hash proposalId)
        {
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentContractTester(bp);
                var approveResult = await tester.Approve.SendAsync(proposalId);
                approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
            }
        }
    }
}