using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Standards.ACS0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.EconomicSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;

namespace AElf.Contracts.Profit;

public class ProfitContractTestBase : ContractTestBase<ProfitContractTestAElfModule>
{
    protected ECKeyPair StarterKeyPair => Accounts[0].KeyPair;
    protected Address Starter => Accounts[0].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs => Accounts.Take(5).Select(a => a.KeyPair).ToList();
    protected Address TokenContractAddress { get; set; }
    protected Address ProfitContractAddress { get; set; }

    protected Address ParliamentContractAddress { get; set; }

    protected Address ConsensusContractAddress { get; set; }

    internal List<ProfitContractImplContainer.ProfitContractImplStub> Creators => CreatorKeyPair
        .Select(p => GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, p))
        .ToList();

    internal List<ProfitContractImplContainer.ProfitContractImplStub> Normal => NormalKeyPair
        .Select(p => GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, p))
        .ToList();

    protected List<ECKeyPair> CreatorKeyPair => Accounts.Skip(1).Take(4).Select(a => a.KeyPair).ToList();

    protected List<ECKeyPair> NormalKeyPair => Accounts.Skip(5).Take(5).Select(a => a.KeyPair).ToList();

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub { get; set; }

    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }

    internal ProfitContractImplContainer.ProfitContractImplStub ProfitContractStub { get; set; }

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

    public ProfitContractTestBase()
    {
        InitializeContracts();
    }

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

        //deploy consensus contract
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
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
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

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateProfitInitializationCallList()
    {
        return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateTokenInitializationCallList()
    {
        const string symbol = "ELF";
        var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
        {
            Symbol = symbol,
            Decimals = 2,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = ProfitContractTestConstants.NativeTokenTotalSupply,
            Issuer = Starter,
            Owner = Starter,
            LockWhiteList =
            {
                ProfitContractAddress
            }
        });

        // For creating `Treasury` profit scheme.
        tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
        {
            Symbol = symbol,
            Amount = (long)(ProfitContractTestConstants.NativeTokenTotalSupply * 0.12),
            To = Address.FromPublicKey(StarterKeyPair.PublicKey),
            Memo = "Issue token to default user for vote.",
        });

        CreatorKeyPair.ForEach(creatorKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
            new IssueInput
            {
                Symbol = symbol,
                Amount = (long)(ProfitContractTestConstants.NativeTokenTotalSupply * 0.1),
                To = Address.FromPublicKey(creatorKeyPair.PublicKey),
                Memo = "set voters few amount for voting."
            }));

        NormalKeyPair.ForEach(normalKeyPair => tokenContractCallList.Add(nameof(TokenContract.Issue),
            new IssueInput
            {
                Symbol = symbol,
                Amount = (long)(ProfitContractTestConstants.NativeTokenTotalSupply * 0.05),
                To = Address.FromPublicKey(normalKeyPair.PublicKey),
                Memo = "set voters few amount for voting."
            }));

        return tokenContractCallList;
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateParliamentInitializationCallList()
    {
        var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        parliamentContractCallList.Add(nameof(ParliamentContractStub.Initialize), new InitializeInput
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
                MinerIncreaseInterval = 31536000
            });

        consensusContractCallList.Add(nameof(AEDPoSContractStub.FirstRound), new MinerList
        {
            Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) }
        }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));

        return consensusContractCallList;
    }
}