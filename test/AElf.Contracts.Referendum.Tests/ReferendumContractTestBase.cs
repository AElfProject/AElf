using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;

namespace AElf.Contracts.Referendum;

public class ReferendumContractTestBase : ContractTestBase<ReferendumContractTestAElfModule>
{
    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(5).Select(a => a.KeyPair).ToList();

    protected Address TokenContractAddress { get; set; }
    protected Address ReferendumContractAddress { get; set; }

    protected Address ParliamentContractAddress { get; set; }

    protected Address ConsensusContractAddress { get; set; }
    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

    protected IBlockTimeProvider BlockTimeProvider =>
        Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal ReferendumContractImplContainer.ReferendumContractImplStub ReferendumContractStub { get; set; }

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

    protected void InitializeContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

        // deploy token contract
        TokenContractAddress = AsyncHelper.RunSync(() =>
            BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                    Name = TokenSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateTokenInitializationCallList()
                })).Output;
        TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);

        //deploy Referendum contract
        ReferendumContractAddress = AsyncHelper.RunSync(() =>
            BasicContractZeroStub.DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ReferendumContract).Assembly.Location)),
                Name = ReferendumSmartContractAddressNameProvider.Name
                // TransactionMethodCallList = GenerateReferendumInitializationCallList()
            })).Output;
        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);

        //deploy parliament auth contract
        ParliamentContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(DefaultSenderKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentContract).Assembly.Location)),
                    Name = ParliamentSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateParliamentInitializationCallList()
                })).Output;
        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);

        //deploy consensus contract
        ConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(DefaultSenderKeyPair)
            .DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(AEDPoSContract).Assembly.Location)),
                    Name = ConsensusSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateConsensusInitializationCallList()
                })).Output;
        AEDPoSContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
    }

    internal ReferendumContractImplContainer.ReferendumContractImplStub GetReferendumContractTester(ECKeyPair keyPair)
    {
        return GetTester<ReferendumContractImplContainer.ReferendumContractImplStub>(ReferendumContractAddress,
            keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateTokenInitializationCallList()
    {
        const string symbol = "ELF";
        const long totalSupply = 100000000_00000000;
        var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
        {
            Symbol = symbol,
            Decimals = 8,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = totalSupply,
            Issuer = DefaultSender
        });

        //issue default user
        tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
        {
            Symbol = symbol,
            Amount = 10000000_00000000,
            To = DefaultSender,
            Memo = "Issue token to default user."
        });

        //issue some user
        for (var i = 1; i < 6; i++)
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = 100_00000000,
                To = Accounts[i].Address,
                Memo = "Issue token to users"
            });

        return tokenContractCallList;
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateParliamentInitializationCallList()
    {
        var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        parliamentContractCallList.Add(nameof(ParliamentContractStub.Initialize), new InitializeInput
        {
            PrivilegedProposer = DefaultSender,
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

    protected async Task<long> GetBalanceAsync(string symbol, Address owner)
    {
        var balanceResult = await TokenContractStub.GetBalance.CallAsync(
            new GetBalanceInput
            {
                Owner = owner,
                Symbol = symbol
            });
        return balanceResult.Balance;
    }
}