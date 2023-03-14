using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.GovernmentSystem;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Genesis;

public class
    AuthorityNotRequiredBasicContractZeroTestBase : ContractTestKit.ContractTestBase<
        AuthorityNotRequiredBasicContractZeroTestModule>
{
    protected new ISmartContractAddressService ContractAddressService =>
        Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

    internal ACS0Container.ACS0Stub DefaultTester =>
        GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, DefaultSenderKeyPair);

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub ZeroTester =>
        GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, DefaultSenderKeyPair);


    protected ECKeyPair DefaultSenderKeyPair => Accounts.First().KeyPair;
    protected Address DefaultSender => Accounts.First().Address;
    protected ECKeyPair AnotherUserKeyPair => Accounts.Last().KeyPair;
    protected Address AnotherUser => Accounts.Last().Address;

    internal ACS0Container.ACS0Stub AnotherTester =>
        GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, AnotherUserKeyPair);
}

public class BasicContractZeroTestBase : TestBase.ContractTestBase<BasicContractZeroTestAElfModule>
{
    protected readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;

    protected readonly ECKeyPair TesterKeyPair;
    protected Address AssociationContractAddress;
    protected long BalanceOfStarter;
    protected Address BasicContractZeroAddress;
    protected Address ParliamentAddress;

    protected Address SideBasicContractZeroAddress;
    protected ContractTester<BasicContractZeroTestAElfModule> SideChainMinerTester;

    protected ContractTester<BasicContractZeroTestAElfModule> SideChainTester;
    protected Address SideParliamentAddress;
    protected Address SideTokenContractAddress;
    protected Address TokenContractAddress;

    protected long TotalSupply;

    public BasicContractZeroTestBase()
    {
        TesterKeyPair = Tester.KeyPair;
        AsyncHelper.RunSync(() =>
            Tester.InitialChainAsyncWithAuthAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(),
                out TotalSupply,
                out _,
                out BalanceOfStarter)));

        BasicContractZeroAddress = Tester.GetZeroContractAddress();
        ParliamentAddress = Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
        TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        AssociationContractAddress = Tester.GetContractAddress(AssociationSmartContractAddressNameProvider.Name);
    }

    protected ECKeyPair AnotherUserKeyPair => SampleECKeyPairs.KeyPairs.Last();
    protected ECKeyPair CreatorKeyPair => SampleECKeyPairs.KeyPairs[10];

    protected ECKeyPair AnotherMinerKeyPair => SampleECKeyPairs.KeyPairs[2];

    protected Address AnotherMinerAddress => Address.FromPublicKey(AnotherMinerKeyPair.PublicKey);

    protected void StartSideChain(string sideChainSymbol = "STA")
    {
        var chainId = ChainHelper.ConvertBase58ToChainId("Side");
        var mainChainId = Tester.GetChainAsync().Result.Id;
        SideChainTester =
            new ContractTester<BasicContractZeroTestAElfModule>(chainId, CreatorKeyPair);
        AsyncHelper.RunSync(() =>
            SideChainTester.InitialCustomizedChainAsync(chainId,
                configureSmartContract: SideChainTester.GetSideChainSystemContract(
                    SideChainTester.GetCallOwnerAddress(), mainChainId, sideChainSymbol, out TotalSupply,
                    SideChainTester.GetCallOwnerAddress())));
        SideBasicContractZeroAddress = SideChainTester.GetZeroContractAddress();
        SideTokenContractAddress = SideChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        SideParliamentAddress =
            SideChainTester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);

        SideChainMinerTester = SideChainTester.CreateNewContractTester(SideChainTester.InitialMinerList.First());
    }
    
    protected async Task ApproveWithMinersAsync(
        ContractTester<BasicContractZeroTestAElfModule> tester, Address parliamentContract, Hash proposalId)
    {
        var tester0 = tester.CreateNewContractTester(Tester.InitialMinerList[0]);
        await tester0.ExecuteContractWithMiningAsync(parliamentContract,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), proposalId);

        var tester1 = tester.CreateNewContractTester(Tester.InitialMinerList[1]);
        await tester1.ExecuteContractWithMiningAsync(parliamentContract,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), proposalId);

        var tester2 = tester.CreateNewContractTester(Tester.InitialMinerList[2]);
        await tester2.ExecuteContractWithMiningAsync(parliamentContract,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), proposalId);
    }

    protected async Task<Hash> CreateProposalAsync(ContractTester<BasicContractZeroTestAElfModule> tester,
        Address contractAddress, Address organizationAddress, string methodName, IMessage input)
    {
        var basicContract = tester.GetZeroContractAddress();
        // var organizationAddress = await GetGenesisAddressAsync(tester, parliamentContractAddress);
        var proposal = await tester.ExecuteContractWithMiningAsync(contractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.CreateProposal),
            new CreateProposalInput
            {
                ContractMethodName = methodName,
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString(),
                ToAddress = basicContract,
                OrganizationAddress = organizationAddress
            });
        var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
        return proposalId;
    }

    protected async Task<TransactionResult> ReleaseProposalAsync(
        ContractTester<BasicContractZeroTestAElfModule> tester, Address parliamentContract, Hash proposalId)
    {
        var transactionResult = await tester.ExecuteContractWithMiningAsync(parliamentContract,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Release), proposalId);
        return transactionResult;
    }

    internal async Task<ReleaseContractInput> ProposeContractAsync(
        ContractTester<BasicContractZeroTestAElfModule> tester, string methodName,
        IMessage input)
    {
        var contractDeploymentController = await GetContractDeploymentController(tester, BasicContractZeroAddress);
        var proposalId = await CreateProposalAsync(tester, contractDeploymentController.ContractAddress,
            contractDeploymentController.OwnerAddress, methodName, input);
        await ApproveWithMinersAsync(tester, contractDeploymentController.ContractAddress, proposalId);
        var releaseResult =
            await ReleaseProposalAsync(tester, contractDeploymentController.ContractAddress, proposalId);
        var proposedContractInputHash = CodeCheckRequired.Parser
            .ParseFrom(releaseResult.Logs.First(l => l.Name.Contains(nameof(CodeCheckRequired))).NonIndexed)
            .ProposedContractInputHash;
        var codeCheckProposalId = ProposalCreated.Parser
            .ParseFrom(releaseResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;

        return new ReleaseContractInput
        {
            ProposedContractInputHash = proposedContractInputHash,
            ProposalId = codeCheckProposalId
        };
    }

    internal async Task<TransactionResult> ApproveWithTesterAsync(
        ContractTester<BasicContractZeroTestAElfModule> tester, Address contractAddress, Hash proposalId)
    {
        return await tester.ExecuteContractWithMiningAsync(contractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractStub.Approve), proposalId);
    }

    internal async Task<Address> DeployAsync(ContractTester<BasicContractZeroTestAElfModule> tester,
        Address parliamentContract, ContractDeploymentInput contractDeploymentInput)
    {
        var proposingTxResult = await tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);

        var proposalCreatedEvent =
            proposingTxResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(ProposalCreated)));
        if (proposalCreatedEvent == null)
            return null;

        var proposalId = ProposalCreated.Parser
            .ParseFrom(proposalCreatedEvent.NonIndexed)
            .ProposalId;
        var proposedContractInputHash = ContractProposed.Parser
            .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
            .ProposedContractInputHash;
        await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

        // release contract code and trigger code check proposal
        var releaseApprovedContractTxResult = await tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
            {
                ProposalId = proposalId,
                ProposedContractInputHash = proposedContractInputHash
            });
        var codeCheckProposalId = ProposalCreated.Parser
            .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                .NonIndexed).ProposalId;

        await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

        // release code check proposal and deployment completes
        var deploymentResult = await tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });
        var deploymentEvent = deploymentResult.Logs.FirstOrDefault(l => l.Name.Contains(nameof(ContractDeployed)));

        var address = deploymentEvent != null
            ? ContractDeployed.Parser.ParseFrom(deploymentEvent.NonIndexed).Address
            : null;
        return address;
    }

    protected async Task<Address> CreateOrganizationAsync(ContractTester<BasicContractZeroTestAElfModule> tester,
        Address parliamentContract)
    {
        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = 20000 / tester.InitialMinerList.Count,
                MinimalVoteThreshold = 20000 / tester.InitialMinerList.Count
            }
        };
        var transactionResult =
            await tester.ExecuteContractWithMiningAsync(parliamentContract,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateOrganization),
                createOrganizationInput);
        return Address.Parser.ParseFrom(transactionResult.ReturnValue);
    }

    protected async Task<Address> GetGenesisAddressAsync(ContractTester<BasicContractZeroTestAElfModule> tester,
        Address parliamentContract)
    {
        var organizationAddress = Address.Parser.ParseFrom(await tester.CallContractMethodAsync(
            parliamentContract,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.GetDefaultOrganizationAddress),
            new Empty()));
        return organizationAddress;
    }

    protected byte[] ReadCode(string path)
    {
        return File.Exists(path)
            ? File.ReadAllBytes(path)
            : throw new FileNotFoundException("Contract DLL cannot be found. " + path);
    }

    internal async Task<AuthorityInfo> GetContractDeploymentController<T>(
        ContractTester<T> tester, Address genesisContractAddress) where T : ContractTestAElfModule
    {
        var contractDeploymentControllerByteString = await tester.CallContractMethodAsync(genesisContractAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractDeploymentController),
            new Empty());
        return AuthorityInfo.Parser.ParseFrom(contractDeploymentControllerByteString);
    }

    internal async Task<AuthorityInfo> GetContractCodeCheckController<T>(
        ContractTester<T> tester, Address genesisContractAddress) where T : ContractTestAElfModule
    {
        var contractCodeCheckControllerByteString = await tester.CallContractMethodAsync(genesisContractAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetCodeCheckController), new Empty());
        return AuthorityInfo.Parser.ParseFrom(contractCodeCheckControllerByteString);
    }

    internal async Task<AuthorityInfo> GetMethodFeeController<T>(
        ContractTester<T> tester, Address genesisContractAddress) where T : ContractTestAElfModule
    {
        var methodFeeControllerByteString = await tester.CallContractMethodAsync(genesisContractAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetMethodFeeController), new Empty());
        return AuthorityInfo.Parser.ParseFrom(methodFeeControllerByteString);
    }
}