using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.DApp;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.EconomicSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.TestContract.DApp.InitializeInput;

namespace AElf.Contracts.TokenHolder;

public class TokenHolderContractTestBase : ContractTestBase<TokenHolderContractTestAElfModule>
{
    protected ECKeyPair StarterKeyPair => Accounts[0].KeyPair;
    protected Address Starter => Accounts[0].Address;

    protected ECKeyPair ProfitReceiverKeyPair => Accounts[1].KeyPair;
    protected Address Receiver => Accounts[1].Address;

    protected List<ECKeyPair> UserKeyPairs => Accounts.Skip(2).Take(3).Select(a => a.KeyPair).ToList();

    protected List<Address> UserAddresses =>
        UserKeyPairs.Select(k => Address.FromPublicKey(k.PublicKey)).ToList();

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(TokenHolderContractTestConstants.InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected Address TokenContractAddress { get; set; }
    protected Address ProfitContractAddress { get; set; }
    protected Address ParliamentContractAddress { get; set; }
    protected Address TokenHolderContractAddress { get; set; }
    protected Address DAppContractAddress { get; set; }
    protected Address ConsensusContractAddress { get; set; }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub { get; set; }

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub { get; set; }

    internal ProfitContractImplContainer.ProfitContractImplStub ProfitContractStub { get; set; }

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }

    internal TokenHolderContractImplContainer.TokenHolderContractImplStub TokenHolderContractStub { get; set; }

    internal DAppContainer.DAppStub DAppContractStub { get; set; }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

    protected void InitializeContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(StarterKeyPair);

        ProfitContractAddress = AsyncHelper.RunSync(() =>
            BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ProfitContract).Assembly.Location)),
                    Name = ProfitSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateProfitInitializationCallList()
                })).Output;
        ProfitContractStub = GetProfitContractTester(StarterKeyPair);

        //deploy token holder contract
        TokenHolderContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenHolderContract).Assembly.Location)),
                    Name = TokenHolderSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList =
                        new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                })).Output;
        TokenHolderContractStub = GetTokenHolderContractTester(StarterKeyPair);

        //deploy token contract
        TokenContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                    Name = TokenSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateTokenInitializationCallList()
                })).Output;
        TokenContractStub = GetTokenContractTester(StarterKeyPair);

        //deploy parliament auth contract
        ParliamentContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentContract).Assembly.Location)),
                    Name = ParliamentSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateParliamentInitializationCallList()
                })).Output;
        ParliamentContractStub = GetParliamentContractTester(StarterKeyPair);

        ConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AEDPoSContract).Assembly.Location)),
                    Name = ConsensusSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateConsensusInitializationCallList()
                })).Output;
        AEDPoSContractStub = GetConsensusContractTester(StarterKeyPair);
        
        //deploy DApp contract
        DAppContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(StarterKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(DAppContract).Assembly.Location)),
                    Name = DappSmartContractAddressNameProvider.Name
                })).Output;
        DAppContractStub = GetTester<DAppContainer.DAppStub>(DAppContractAddress, UserKeyPairs.First());
        AsyncHelper.RunSync(TransferToContract);
        AsyncHelper.RunSync(async () =>
        {
            await DAppContractStub.InitializeForUnitTest.SendAsync(new InitializeInput
            {
                ProfitReceiver = Address.FromPublicKey(UserKeyPairs[1].PublicKey)
            });
        });
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
    }

    internal ProfitContractImplContainer.ProfitContractImplStub GetProfitContractTester(ECKeyPair keyPair)
    {
        return GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal TokenHolderContractImplContainer.TokenHolderContractImplStub GetTokenHolderContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<TokenHolderContractImplContainer.TokenHolderContractImplStub>(TokenHolderContractAddress,
            keyPair);
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateProfitInitializationCallList()
    {
        return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
    }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateTokenInitializationCallList()
    {
        var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
        {
            Symbol = TokenHolderContractTestConstants.NativeTokenSymbol,
            Decimals = 8,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = TokenHolderContractTestConstants.NativeTokenTotalSupply,
            Issuer = Starter,
            LockWhiteList =
            {
                ProfitContractAddress,
                TokenHolderContractAddress
            }
        });
        tokenContractCallList.Add(nameof(TokenContract.SetPrimaryTokenSymbol),
            new SetPrimaryTokenSymbolInput { Symbol = "ELF" });
        tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
        {
            Symbol = TokenHolderContractTestConstants.NativeTokenSymbol,
            Amount = (long)(TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.12),
            To = Address.FromPublicKey(StarterKeyPair.PublicKey),
            Memo = "Issue token to default user for vote."
        });

        UserKeyPairs.ForEach(creatorKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
            new IssueInput
            {
                Symbol = TokenHolderContractTestConstants.NativeTokenSymbol,
                Amount = (long)(TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1),
                To = Address.FromPublicKey(creatorKeyPair.PublicKey),
                Memo = "set voters few amount for voting."
            }));

        return tokenContractCallList;
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateParliamentInitializationCallList()
    {
        var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        parliamentContractCallList.Add(nameof(ParliamentContractStub.Initialize), new Parliament.InitializeInput
        {
            PrivilegedProposer = Starter,
            ProposerAuthorityRequired = true
        });

        return parliamentContractCallList;
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateConsensusInitializationCallList()
    {
        var consensusContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        consensusContractCallList.Add(nameof(AEDPoSContractStub.InitialAElfConsensusContract),
            new InitialAElfConsensusContractInput
            {
                PeriodSeconds = 604800L,
                MinerIncreaseInterval = 31536000,
                IsSideChain = true
            });

        consensusContractCallList.Add(nameof(AEDPoSContractStub.FirstRound), new MinerList
        {
            Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) }
        }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));

        return consensusContractCallList;
    }

    protected async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
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

    protected async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
            approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        }
    }
    
    private async Task TransferToContract()
    {
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            To = DAppContractAddress,
            Amount = 10000_0000000000,
            Symbol = TokenHolderContractTestConstants.NativeTokenSymbol,
        });
    }
}