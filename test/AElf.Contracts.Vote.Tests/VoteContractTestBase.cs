using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestContract.VirtualAddress;
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
using Volo.Abp.Threading;

namespace AElf.Contracts.Vote;

public class VoteContractTestBase : ContractTestBase<VoteContractTestAElfModule>
{
    protected const string TestTokenSymbol = "ELF";
    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(5).Select(a => a.KeyPair).ToList();

    protected Address TokenContractAddress { get; set; }
    protected Address VoteContractAddress { get; set; }
    protected Address ParliamentContractAddress { get; set; }
    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
    protected Address ConsensusContractAddress { get; set; }
    protected Address VirtualAddressContractAddress { get; set; }
    internal ACS0Container.ACS0Stub BasicContractZeroStub { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal VoteContractImplContainer.VoteContractImplStub VoteContractStub { get; set; }
    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }
    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }
    internal VirtualAddressContractContainer.VirtualAddressContractStub VirtualAddressContractStub { get; set; }

    protected void InitializeContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

        //deploy vote contract
        VoteContractAddress = AsyncHelper.RunSync(() =>
            BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(VoteContract).Assembly.Location)),
                    Name = VoteSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateVoteInitializationCallList()
                })).Output;
        VoteContractStub = GetVoteContractTester(DefaultSenderKeyPair);

        //deploy token contract
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

        //deploy parliament auth contract
        ParliamentContractAddress = AsyncHelper.RunSync(() =>
            BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentContract).Assembly.Location)),
                    Name = ParliamentSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateParliamentInitializationCallList()
                })).Output;
        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);

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
        
        VirtualAddressContractAddress = AsyncHelper.RunSync(async () =>
            await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("VirtualAddress")).Value,
                HashHelper.ComputeFrom("AElf.Contracts.TestContract.VirtualAddress"),
                DefaultSenderKeyPair));
        VirtualAddressContractStub = GetTester<VirtualAddressContractContainer.VirtualAddressContractStub>(VirtualAddressContractAddress, DefaultSenderKeyPair);
    }

    internal ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
    }

    internal VoteContractImplContainer.VoteContractImplStub GetVoteContractTester(ECKeyPair keyPair)
    {
        return GetTester<VoteContractImplContainer.VoteContractImplStub>(VoteContractAddress, keyPair);
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

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateVoteInitializationCallList()
    {
        return new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
    {
        const long totalSupply = 1_000_000_000_0000_0000;

        var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        tokenContractCallList.Add(nameof(TokenContract.Create), new CreateInput
        {
            Symbol = TestTokenSymbol,
            Decimals = 2,
            IsBurnable = true,
            TokenName = "elf token for testing",
            TotalSupply = totalSupply,
            Issuer = DefaultSender,
            Owner = DefaultSender,
            LockWhiteList =
            {
                VoteContractAddress
            }
        });

        //issue default user
        tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
        {
            Symbol = TestTokenSymbol,
            Amount = totalSupply - 20 * 100_000_0000_0000L,
            To = DefaultSender,
            Memo = "Issue token to default user for vote."
        });

        //issue some amount to voter
        for (var i = 1; i < 20; i++)
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = TestTokenSymbol,
                Amount = 100_000_0000_0000L,
                To = Accounts[i].Address,
                Memo = "set voters few amount for voting."
            });

        return tokenContractCallList;
    }

    private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
        GenerateParliamentInitializationCallList()
    {
        var parliamentContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
        parliamentContractCallList.Add(nameof(ParliamentContract.Initialize), new InitializeInput
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

        consensusContractCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound), new MinerList
        {
            Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) }
        }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));

        return consensusContractCallList;
    }

    protected long GetUserBalance(Address owner)
    {
        return TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = owner,
            Symbol = TestTokenSymbol
        }).Result.Balance;
    }
}