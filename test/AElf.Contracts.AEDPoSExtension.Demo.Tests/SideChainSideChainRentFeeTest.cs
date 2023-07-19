using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

public class SideChainSideChainRentFeeTest : SideChainRentFeeTestBase<SideChainRentFeeModule>
{
    private const int CpuAmount = 4;
    private const int RamAmount = 8;
    private const int DiskAmount = 512;
    private const int NetAmount = 1000;

    private const long ResourceSupply = 1_0000_0000_00000000;

    private const long Rental = 100;

    public SideChainSideChainRentFeeTest()
    {
        DeployContracts();
    }

    private Address Creator => Accounts[0].Address;

    private static List<string> Symbols => new() { "CPU", "RAM", "DISK", "NET" };

    [Fact]
    public async Task SideChainRentalController_Test()
    {
        var rentalControllerRet =
            await TokenContractStub.GetSideChainRentalControllerCreateInfo.SendWithExceptionAsync(new Empty());
        rentalControllerRet.TransactionResult.Error.ShouldContain("side chain creator dose not exist");
        await InitialTokenContractAsync();
        var rentalController =
            await TokenContractStub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty());
        rentalController.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetRentalInfo_Test()
    {
        await InitialTokenContractAsync();
        var rentalUnitPriceInfo = await TokenContractStub.GetOwningRentalUnitValue.CallAsync(new Empty());
        rentalUnitPriceInfo.ResourceUnitValue.All(x => x.Value == Rental).ShouldBeTrue();
        var resourceUsageInfo = (await TokenContractStub.GetResourceUsage.CallAsync(new Empty())).Value;
        resourceUsageInfo["CPU"].ShouldBe(CpuAmount);
        resourceUsageInfo["RAM"].ShouldBe(RamAmount);
        resourceUsageInfo["DISK"].ShouldBe(DiskAmount);
        resourceUsageInfo["NET"].ShouldBe(NetAmount);
    }

    [Fact]
    public async Task ChargeRentalTest()
    {
        await InitialTokenContractAsync();

        // Check balance before mining
        {
            var cpuBalance = await GetCreatorBalanceOfAsync("CPU");
            cpuBalance.ShouldBe(ResourceSupply);
            var ramBalance = await GetCreatorBalanceOfAsync("RAM");
            ramBalance.ShouldBe(ResourceSupply);
            var diskBalance = await GetCreatorBalanceOfAsync("DISK");
            diskBalance.ShouldBe(ResourceSupply);
            var netBalance = await GetCreatorBalanceOfAsync("NET");
            netBalance.ShouldBe(ResourceSupply);
        }

        // charge every 1 minute
        await DelayOneMinuteAsync();

        // Check balance before mining
        {
            var cpuBalance = await GetCreatorBalanceOfAsync("CPU");
            cpuBalance.ShouldBe(ResourceSupply - CpuAmount * Rental);
            var ramBalance = await GetCreatorBalanceOfAsync("RAM");
            ramBalance.ShouldBe(ResourceSupply - RamAmount * Rental);
            var diskBalance = await GetCreatorBalanceOfAsync("DISK");
            diskBalance.ShouldBe(ResourceSupply - DiskAmount * Rental);
            var netBalance = await GetCreatorBalanceOfAsync("NET");
            netBalance.ShouldBe(ResourceSupply - NetAmount * Rental);
        }
    }

    [Fact]
    public async Task OwnResourceTest()
    {
        await InitialTokenContractAsync(false);

        // Check balance before mining
        {
            var cpuBalance = await GetCreatorBalanceOfAsync("CPU");
            cpuBalance.ShouldBe(0);
            var ramBalance = await GetCreatorBalanceOfAsync("RAM");
            ramBalance.ShouldBe(0);
            var diskBalance = await GetCreatorBalanceOfAsync("DISK");
            diskBalance.ShouldBe(0);
            var netBalance = await GetCreatorBalanceOfAsync("NET");
            netBalance.ShouldBe(0);
        }

        await DelayOneMinuteAsync();

        var owningRental = await TokenContractStub.GetOwningRental.CallAsync(new Empty());
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
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                To = Creator,
                Amount = ResourceSupply
            });

        var beforeConsensusCpuBalance = await GetBalanceOfConsensusAsync("CPU");
        var beforeConsensusRamBalance = await GetBalanceOfConsensusAsync("RAM");
        var beforeConsensusDiskBalance = await GetBalanceOfConsensusAsync("DISK");
        var beforeConsensusNetBalance = await GetBalanceOfConsensusAsync("NET");
        await DelayOneMinuteAsync();

        var owningRental = await TokenContractStub.GetOwningRental.CallAsync(new Empty());
        owningRental.ResourceAmount["CPU"].ShouldBe(0);
        owningRental.ResourceAmount["RAM"].ShouldBe(0);
        owningRental.ResourceAmount["DISK"].ShouldBe(0);
        owningRental.ResourceAmount["NET"].ShouldBe(0);

        // Check balance before mining
        {
            var cpuBalance = await GetCreatorBalanceOfAsync("CPU");
            cpuBalance.ShouldBe(ResourceSupply - CpuAmount * Rental * 2);
            var ramBalance = await GetCreatorBalanceOfAsync("RAM");
            ramBalance.ShouldBe(ResourceSupply - RamAmount * Rental * 2);
            var diskBalance = await GetCreatorBalanceOfAsync("DISK");
            diskBalance.ShouldBe(ResourceSupply - DiskAmount * Rental * 2);
            var netBalance = await GetCreatorBalanceOfAsync("NET");
            netBalance.ShouldBe(ResourceSupply - NetAmount * Rental * 2);
        }

        {
            var afterConsensusCpuBalance = await GetBalanceOfConsensusAsync("CPU");
            var afterConsensusRamBalance = await GetBalanceOfConsensusAsync("RAM");
            var afterConsensusDiskBalance = await GetBalanceOfConsensusAsync("DISK");
            var afterConsensusNetBalance = await GetBalanceOfConsensusAsync("NET");

            (afterConsensusCpuBalance - beforeConsensusCpuBalance).ShouldBe(CpuAmount * Rental * 2);
            (afterConsensusRamBalance - beforeConsensusRamBalance).ShouldBe(RamAmount * Rental * 2);
            (afterConsensusDiskBalance - beforeConsensusDiskBalance).ShouldBe(DiskAmount * Rental * 2);
            (afterConsensusNetBalance - beforeConsensusNetBalance).ShouldBe(NetAmount * Rental * 2);
        }
    }

    [Fact]
    public async Task PayDebtTest_NotEnough()
    {
        await OwnResourceTest();

        // Charge
        foreach (var symbol in Symbols)
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                To = Creator,
                Amount = 1
            });

        await DelayOneMinuteAsync();

        var owningRental = await TokenContractStub.GetOwningRental.CallAsync(new Empty());
        owningRental.ResourceAmount["CPU"].ShouldBe(CpuAmount * Rental * 2 - 1);
        owningRental.ResourceAmount["RAM"].ShouldBe(RamAmount * Rental * 2 - 1);
        owningRental.ResourceAmount["DISK"].ShouldBe(DiskAmount * Rental * 2 - 1);
        owningRental.ResourceAmount["NET"].ShouldBe(NetAmount * Rental * 2 - 1);

        // Check balance before mining
        {
            var cpuBalance = await GetCreatorBalanceOfAsync("CPU");
            cpuBalance.ShouldBe(0);
            var ramBalance = await GetCreatorBalanceOfAsync("RAM");
            ramBalance.ShouldBe(0);
            var diskBalance = await GetCreatorBalanceOfAsync("DISK");
            diskBalance.ShouldBe(0);
            var netBalance = await GetCreatorBalanceOfAsync("NET");
            netBalance.ShouldBe(0);
        }
    }

    [Fact]
    public async Task ChangeSIdeChainRentalController_Fail()
    {
        await InitialTokenContractAsync();
        var updateRet =
            await TokenContractStub.ChangeSideChainRentalController.SendWithExceptionAsync(new AuthorityInfo());
        updateRet.TransactionResult.Error.ShouldContain("no permission");

        var authority = new AuthorityInfo
        {
            ContractAddress = AssociationContractAddress,
            OwnerAddress = ConsensusContractAddress
        };

        var invalidInputRet = await UpdateSideChainRentalDefaultProposalWithExceptionAsync(
            nameof(TokenContractImplContainer.TokenContractImplStub.ChangeSideChainRentalController), authority);
        invalidInputRet.Error.ShouldContain("new controller does not exist");
    }

    [Fact]
    public async Task ChangeSIdeChainRentalController_Success()
    {
        await InitialTokenContractAsync();
        var member = Accounts[0].Address;
        var proposers = new List<Address> { member };
        var newOrganizationCreationInput = new CreateOrganizationInput
        {
            OrganizationMemberList = new OrganizationMemberList
            {
                OrganizationMembers = { proposers }
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
                Proposers = { proposers }
            }
        };
        var createNewAssociationOrganization =
            await AssociationContractStub.CreateOrganization.SendAsync(newOrganizationCreationInput);
        var newControllerAddress = new Address();
        newControllerAddress.MergeFrom(createNewAssociationOrganization.TransactionResult.ReturnValue);
        var authority = new AuthorityInfo
        {
            ContractAddress = AssociationContractAddress,
            OwnerAddress = newControllerAddress
        };

        await UpdateSideChainRentalDefaultProposalAsync(
            nameof(TokenContractImplContainer.TokenContractImplStub.ChangeSideChainRentalController), authority);
        var updateParam = new UpdateRentedResourcesInput();
        var symbolDic = new Dictionary<string, int> { ["CPU"] = 101 };
        updateParam.ResourceAmount.Add(symbolDic);
        var updateProposal = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.UpdateRentedResources),
            Params = updateParam.ToByteString(),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = newControllerAddress
        };
        var updateProposalRet = (await AssociationContractStub.CreateProposal.SendAsync(updateProposal))
            .TransactionResult;
        var updateProposalId = new Hash();
        updateProposalId.MergeFrom(updateProposalRet.ReturnValue);
        await AssociationContractStub.Approve.SendAsync(updateProposalId);
        await AssociationContractStub.Release.SendAsync(updateProposalId);
        var resourceUsage = await TokenContractStub.GetResourceUsage.CallAsync(new Empty());
        resourceUsage.Value["CPU"].ShouldBe(101);
    }

    private async Task DelayOneMinuteAsync()
    {
        var times = 6;
        while (times-- > 0)
            await TokenContractStub.GetBalance.SendAsync(new GetBalanceInput // each tx set 10 seconds
            {
                Owner = Creator,
                Symbol = "CPU"
            });
    }

    private async Task InitialTokenContractAsync(bool issueToken = true)
    {
        var defaultParliamentOrganization =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        await CreateTokenAsync("ELF", ResourceSupply, issueToken, defaultParliamentOrganization);
        await CreateTokenAsync("CPU", ResourceSupply, issueToken, defaultParliamentOrganization);
        await CreateTokenAsync("RAM", ResourceSupply, issueToken, defaultParliamentOrganization);
        await CreateTokenAsync("DISK", ResourceSupply, issueToken, defaultParliamentOrganization);
        await CreateTokenAsync("NET", ResourceSupply, issueToken, defaultParliamentOrganization);
        var setSideChainCreatorProposalInput = new InitializeFromParentChainInput
        {
            ResourceAmount =
            {
                { "CPU", CpuAmount },
                { "RAM", RamAmount },
                { "DISK", DiskAmount },
                { "NET", NetAmount }
            },
            Creator = Creator
        };
        await ParliamentReachAnAgreementAsync(TokenContractAddress, defaultParliamentOrganization,
            nameof(TokenContractImplContainer.TokenContractImplStub.InitializeFromParentChain),
            setSideChainCreatorProposalInput);

        var updateRentalInput = new UpdateRentalInput
        {
            Rental =
            {
                { "CPU", Rental },
                { "RAM", Rental },
                { "DISK", Rental },
                { "NET", Rental }
            }
        };
        await TokenContractStub.SetPrimaryTokenSymbol.SendAsync(new SetPrimaryTokenSymbolInput
            { Symbol = NativeTokenSymbol });
        await TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty());
        await UpdateSideChainRentalDefaultProposalAsync(
            nameof(TokenContractImplContainer.TokenContractImplStub.UpdateRental), updateRentalInput);
    }

    private async Task CreateTokenAsync(string symbol, long supply, bool issueToken, Address organizationAddress)
    {
        await ParliamentReachAnAgreementAsync(TokenContractAddress, organizationAddress,
            nameof(TokenContractStub.Create), new CreateInput
            {
                Decimals = 8,
                Issuer = Creator,
                Symbol = symbol,
                TotalSupply = supply,
                IsBurnable = true,
                TokenName = $"{symbol} token.",
                Owner = Creator
            });
        
        if (!issueToken) return;

        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = symbol,
            To = Creator,
            Amount = supply
        });
    }

    private async Task<long> GetCreatorBalanceOfAsync(string symbol)
    {
        return (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = Creator,
            Symbol = symbol
        })).Balance;
    }

    private async Task<long> GetBalanceOfConsensusAsync(string symbol)
    {
        return (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = ConsensusContractAddress,
            Symbol = symbol
        })).Balance;
    }

    private async Task ParliamentReachAnAgreementAsync(Address contractAddress, Address organizationAddress,
        string methodName, IMessage input)
    {
        var proposalId = await CreateFeeProposalAsync(contractAddress, organizationAddress, methodName, input);
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
    }

    private async Task<Hash> CreateFeeProposalAsync(Address contractAddress, Address organizationAddress,
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

    private async Task<Hash> CreateApproveAndToSideChainRentalDefaultProposalAsync(string methodName,
        IMessage message)
    {
        var defaultController =
            await TokenContractStub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty());
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var toAssociationProposal = new CreateProposalInput
        {
            ToAddress = TokenContractAddress,
            ContractMethodName = methodName,
            Params = message.ToByteString(),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = defaultController.OwnerAddress
        };
        var associationProposalRet = (await AssociationContractStub.CreateProposal.SendAsync(toAssociationProposal))
            .TransactionResult;
        var associationProposalId = new Hash();
        associationProposalId.MergeFrom(associationProposalRet.ReturnValue);
        await ParliamentReachAnAgreementAsync(AssociationContractAddress, defaultParliamentAddress,
            nameof(AssociationContractImplContainer.AssociationContractImplStub.Approve), associationProposalId);
        await AssociationContractStub.Approve.SendAsync(associationProposalId);
        return associationProposalId;
    }

    private async Task UpdateSideChainRentalDefaultProposalAsync(string methodName,
        IMessage message)
    {
        var associationProposalId = await CreateApproveAndToSideChainRentalDefaultProposalAsync(methodName, message);
        await AssociationContractStub.Release.SendAsync(associationProposalId);
    }

    private async Task<TransactionResult> UpdateSideChainRentalDefaultProposalWithExceptionAsync(string methodName,
        IMessage message)
    {
        var associationProposalId = await CreateApproveAndToSideChainRentalDefaultProposalAsync(methodName, message);
        return (await AssociationContractStub.Release.SendWithExceptionAsync(associationProposalId)).TransactionResult;
    }
}