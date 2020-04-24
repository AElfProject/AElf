using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using CreateOrganizationInput = AElf.Contracts.Association.CreateOrganizationInput;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class DonateResourceTokenTest : AEDPoSExtensionDemoTestBase
    {
        private const int CpuAmount = 4;
        private const int RamAmount = 8;
        private const int DiskAmount = 512;
        private const int NetAmount = 1000;

        private const long Rental = 100;

        private const long ResourceSupply = 1_0000_0000_00000000;

        private static Address Creator => Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey);

        private static List<string> Symbols => new List<string> {"CPU", "RAM", "DISK", "NET"};

        [Fact]
        public async Task ChargeRentalTest()
        {
            await InitialTokenContract();

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(ResourceSupply);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(ResourceSupply);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(ResourceSupply);
                var netBalance = await GetCreatorBalanceOf("NET");
                netBalance.ShouldBe(ResourceSupply);
            }

            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(ResourceSupply - CpuAmount * Rental);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(ResourceSupply - RamAmount * Rental);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(ResourceSupply - DiskAmount * Rental);
                var netBalance = await GetCreatorBalanceOf("NET");
                netBalance.ShouldBe(ResourceSupply - NetAmount * Rental);
            }
        }

        [Fact]
        public async Task OwnResourceTest()
        {
            await InitialTokenContract(false);

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(0);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(0);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(0);
                var netBalance = await GetCreatorBalanceOf("NET");
                netBalance.ShouldBe(0);
            }

            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();

            var owningRental = await TokenStub.GetOwningRental.CallAsync(new Empty());
            owningRental.ResourceAmount["CPU"].ShouldBe(CpuAmount * Rental);
            owningRental.ResourceAmount["RAM"].ShouldBe(RamAmount * Rental);
            owningRental.ResourceAmount["DISK"].ShouldBe(DiskAmount * Rental);
            owningRental.ResourceAmount["NET"].ShouldBe(NetAmount * Rental);
        }

        [Fact]
        public async Task PayDebtTest()
        {
            await OwnResourceTest();

            // Charge
            foreach (var symbol in Symbols)
            {
                await TokenStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    To = Creator,
                    Amount = ResourceSupply
                });
            }

            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();

            var owningRental = await TokenStub.GetOwningRental.CallAsync(new Empty());
            owningRental.ResourceAmount["CPU"].ShouldBe(0);
            owningRental.ResourceAmount["RAM"].ShouldBe(0);
            owningRental.ResourceAmount["DISK"].ShouldBe(0);
            owningRental.ResourceAmount["NET"].ShouldBe(0);

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(ResourceSupply - CpuAmount * Rental * 2);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(ResourceSupply - RamAmount * Rental * 2);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(ResourceSupply - DiskAmount * Rental * 2);
                var netBalance = await GetCreatorBalanceOf("NET");
                netBalance.ShouldBe(ResourceSupply - NetAmount * Rental * 2);
            }
        }

        [Fact]
        public async Task PayDebtTest_NotEnough()
        {
            await OwnResourceTest();

            // Charge
            foreach (var symbol in Symbols)
            {
                await TokenStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    To = Creator,
                    Amount = 1
                });
            }

            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();
            await BlockMiningService.MineBlockToNextRoundAsync();

            var owningRental = await TokenStub.GetOwningRental.CallAsync(new Empty());
            owningRental.ResourceAmount["CPU"].ShouldBe(CpuAmount * Rental * 2 - 1);
            owningRental.ResourceAmount["RAM"].ShouldBe(RamAmount * Rental * 2 - 1);
            owningRental.ResourceAmount["DISK"].ShouldBe(DiskAmount * Rental * 2 - 1);
            owningRental.ResourceAmount["NET"].ShouldBe(NetAmount * Rental * 2 - 1);

            // Check balance before mining
            {
                var cpuBalance = await GetCreatorBalanceOf("CPU");
                cpuBalance.ShouldBe(0);
                var ramBalance = await GetCreatorBalanceOf("RAM");
                ramBalance.ShouldBe(0);
                var diskBalance = await GetCreatorBalanceOf("DISK");
                diskBalance.ShouldBe(0);
                var netBalance = await GetCreatorBalanceOf("NET");
                diskBalance.ShouldBe(0);
            }
        }

        [Fact]
        public async Task Authorization_Transfer_Success()
        {
            await InitialTokenContract();
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            var member = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey);
            var proposers = new List<Address> {member};
            var newOrganizationCreationInput = new Association.CreateOrganizationInput
            {
                OrganizationMemberList = new Association.OrganizationMemberList
                {
                    OrganizationMembers = {proposers}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = proposers.Count,
                    MinimalVoteThreshold = proposers.Count,
                    MaximalRejectionThreshold = 0,
                    MaximalAbstentionThreshold = 0
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {proposers}
                }
            };
            var createNewAssociationOrganization = await AssociationStub.CreateOrganization.SendAsync(newOrganizationCreationInput);
            var newControllerAddress = new Address();
            newControllerAddress.MergeFrom(createNewAssociationOrganization.TransactionResult.ReturnValue);
            var authority = new Acs1.AuthorityInfo
            {
                ContractAddress = ContractAddresses[AssociationSmartContractAddressNameProvider.Name],
                OwnerAddress = newControllerAddress
            };
            var parliamentOrgAddress = defaultOrganizationAddress;
            var currentController = await TokenStub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty()); 
            var toAssociationProposal = new CreateProposalInput
            {
                ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                // ContractMethodName = nameof(TokenContractContainer.TokenContractStub.ChangeSideChainParliamentController),
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.ChangeSideChainRentalController),
                Params = authority.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = currentController.OwnerAddress
            };
            var associationProposalRet = (await AssociationStub.CreateProposal.SendAsync(toAssociationProposal)).TransactionResult;
            var associationProposalId = new Hash();
            associationProposalId.MergeFrom(associationProposalRet.ReturnValue);
            
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[AssociationSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                Params = associationProposalId.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = parliamentOrgAddress
            });
            await AssociationStub.Approve.SendAsync(associationProposalId);
            await AssociationStub.Release.SendAsync(associationProposalId);
            var updateParam = new UpdateRentedResourcesInput();
            var symbolDic = new Dictionary<string, int> {["CPU"] = 101};
            updateParam.ResourceAmount.Add(symbolDic);
            var updateProposal = new CreateProposalInput
            {
                ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.UpdateRentedResources),
                Params = updateParam.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = newControllerAddress
            };
            var updateProposalRet = (await AssociationStub.CreateProposal.SendAsync(updateProposal)).TransactionResult;
            var updateProposalId = new Hash();
            updateProposalId.MergeFrom(updateProposalRet.ReturnValue);
            await AssociationStub.Approve.SendAsync(updateProposalId);
            await AssociationStub.Release.SendAsync(updateProposalId);
            var resourceUsage = await TokenStub.GetResourceUsage.CallAsync(new Empty());
            resourceUsage.Value["CPU"].ShouldBe(101);
        }
        private async Task InitialTokenContract(bool issueToken = true)
        {
            if (!ParliamentStubs.Any())
            {
                InitialAcs3Stubs();
            }

            await ParliamentStubs.First().Initialize.SendAsync(new Parliament.InitializeInput
            {
            });
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            var tokenSymbol = GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>().Value
                .ContextVariables[ContextVariableDictionary.NativeSymbolName];
            await CreateToken(tokenSymbol, ResourceSupply, true);

            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.InitializeFromParentChain),
                Params = new InitializeFromParentChainInput()
                {
                    ResourceAmount =
                    {
                        {"CPU", CpuAmount},
                        {"RAM", RamAmount},
                        {"DISK", DiskAmount},
                        {"NET", NetAmount}
                    },
                    Creator = Creator
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });
            
            var updateRentalInput = new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", Rental},
                    {"RAM", Rental},
                    {"DISK", Rental},
                    {"NET", Rental},
                }
            };
            await TokenStub.SetPrimaryTokenSymbol.SendAsync(new SetPrimaryTokenSymbolInput{Symbol = tokenSymbol});
            await TokenStub.InitializeAuthorizedController.SendAsync(new Empty());
            var parliamentOrgAddress = defaultOrganizationAddress;
            var rentalController = await TokenStub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty());       
            var associationAddress = rentalController.OwnerAddress;       
            var toAssociationProposal = new CreateProposalInput
            {
                ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.UpdateRental),
                Params = updateRentalInput.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = associationAddress
            };
            var associationProposalRet = (await AssociationStub.CreateProposal.SendAsync(toAssociationProposal)).TransactionResult;
            var associationProposalId = new Hash();
            associationProposalId.MergeFrom(associationProposalRet.ReturnValue);
            
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[AssociationSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                Params = associationProposalId.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = parliamentOrgAddress
            });
            await AssociationStub.Approve.SendAsync(associationProposalId);
            await AssociationStub.Release.SendAsync(associationProposalId);
            
            await CreateToken("CPU", ResourceSupply, issueToken);
            await CreateToken("RAM", ResourceSupply, issueToken);
            await CreateToken("DISK", ResourceSupply, issueToken);
            await CreateToken("NET", ResourceSupply, issueToken);

        }

        private async Task CreateToken(string symbol, long supply, bool issueToken)
        {
            await TokenStub.Create.SendAsync(new CreateInput
            {
                Decimals = 8,
                Issuer = Creator,
                Symbol = symbol,
                TotalSupply = supply,
                IsBurnable = true,
                TokenName = $"{symbol} token."
            });

            if (!issueToken)
            {
                return;
            }

            await TokenStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                To = Creator,
                Amount = supply
            });
        }

        private async Task<long> GetCreatorBalanceOf(string symbol)
        {
            return (await TokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Creator,
                Symbol = symbol
            })).Balance;
        }
    }
}