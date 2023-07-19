using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestBase;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Parliament;

public class ParliamentContractTestBase : ContractTestKit.ContractTestBase<ParliamentContractTestAElfModule>
{
    protected const int MinersCount = 3;
    protected const int MiningInterval = 4000;
    protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

    protected byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
    protected byte[] ParliamentCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
    protected byte[] DPoSConsensusCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;

    protected ECKeyPair DefaultSenderKeyPair => Accounts[10].KeyPair;
    protected ECKeyPair TesterKeyPair => Accounts[MinersCount + 1].KeyPair;
    protected List<ECKeyPair> InitialMinersKeyPairs => Accounts.Take(MinersCount).Select(a => a.KeyPair).ToList();
    protected Address DefaultSender => Accounts[10].Address;
    protected Address Tester => Accounts[MinersCount + 1].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(3).Select(a => a.KeyPair).ToList();

    protected Address TokenContractAddress { get; set; }
    protected Address ConsensusContractAddress { get; set; }
    protected Address ParliamentContractAddress { get; set; }
    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

    protected IBlockTimeProvider BlockTimeProvider =>
        Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractStub { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractStub ConsensusContractStub { get; set; }
    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub { get; set; }
    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }


    protected void InitializeContracts()
    {
        //get basic stub
        BasicContractStub =
            GetContractZeroTester(DefaultSenderKeyPair);

        //deploy Parliament contract
        ParliamentContractAddress = AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                ParliamentCode,
                ParliamentSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair
            ));
        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        AsyncHelper.RunSync(() => ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            ProposerAuthorityRequired = false,
            PrivilegedProposer = DefaultSender
        }));

        ConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
            KernelConstants.CodeCoverageRunnerCategory,
            DPoSConsensusCode,
            ConsensusSmartContractAddressNameProvider.Name,
            DefaultSenderKeyPair));
        ConsensusContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitializeConsensusAsync());
        
        //deploy token contract
        TokenContractAddress = AsyncHelper.RunSync(() =>
            DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                TokenContractCode,
                TokenSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair));
        TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitializeTokenAsync());
    }


    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal AEDPoSContractContainer.AEDPoSContractStub GetConsensusContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
    }

    internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    private async Task InitializeTokenAsync()
    {
        const string symbol = "ELF";
        const long totalSupply = 100_000_000;

        var organizationAddress =
            (await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty()));
        var approveProposalId = await CreateParliamentProposalAsync(nameof(TokenContractStub.Create), organizationAddress, new CreateInput
        {
            Symbol = symbol,
            Decimals = 2,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = totalSupply,
            Issuer = DefaultSender,
            Owner = DefaultSender
        }, TokenContractAddress);
        await ApproveWithMinersAsync(approveProposalId);
        await ParliamentContractStub.Release.SendAsync(approveProposalId);


        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = symbol,
            Amount = totalSupply - 20 * 100_000L,
            To = DefaultSender,
            Memo = "Issue token to default user."
        });
    }

    private async Task InitializeConsensusAsync()
    {
        await ConsensusContractStub.InitialAElfConsensusContract.SendAsync(new InitialAElfConsensusContractInput
        {
            IsTermStayOne = true
        });
        var minerList = new MinerList
            { Pubkeys = { InitialMinersKeyPairs.Select(m => ByteStringHelper.FromHexString(m.PublicKey.ToHex())) } };
        await ConsensusContractStub.FirstRound.SendAsync(
            minerList.GenerateFirstRoundOfNewTerm(MiningInterval, BlockchainStartTime));
    }

    internal async Task InitializeParliamentContracts()
    {
        await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            ProposerAuthorityRequired = false,
            PrivilegedProposer = DefaultSender
        });
    }

    internal async Task<Hash> CreateParliamentProposalAsync(string method, Address organizationAddress,
        IMessage input, Address toAddress = null)
    {
        var proposal = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ToAddress = toAddress,
            ContractMethodName = method,
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = organizationAddress,
            Params = input.ToByteString()
        })).Output;
        return proposal;
    }
    
    internal async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            await tester.Approve.SendAsync(proposalId);
        }
    }
}

public class ParliamentContractPrivilegeTestBase : TestBase.ContractTestBase<ParliamentContractPrivilegeTestAElfModule>
{
    protected long BalanceOfStarter;
    protected Address ParliamentAddress;
    protected new ContractTester<ParliamentContractPrivilegeTestAElfModule> Tester;
    protected Address TokenContractAddress;

    protected long TotalSupply;


    public ParliamentContractPrivilegeTestBase()
    {
        var mainChainId = ChainHelper.ConvertBase58ToChainId("AELF");
        var chainId = ChainHelper.GetChainId(1);
        Tester = new ContractTester<ParliamentContractPrivilegeTestAElfModule>(chainId, SampleECKeyPairs.KeyPairs[1]);
        AsyncHelper.RunSync(() =>
            Tester.InitialChainAsyncWithAuthAsync(Tester.GetSideChainSystemContract(
                Tester.GetCallOwnerAddress(),
                mainChainId, "STA", out TotalSupply, Tester.GetCallOwnerAddress())));
        ParliamentAddress = Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
        TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
    }

    internal TransferInput TransferInput(Address address)
    {
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = address,
            Memo = "Transfer"
        };
        return transferInput;
    }

    internal CreateProposalInput CreateProposalInput(IMessage input, Address organizationAddress)
    {
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Transfer),
            ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
            Params = input.ToByteString(),
            ToAddress = TokenContractAddress,
            OrganizationAddress = organizationAddress
        };
        return createProposalInput;
    }

    internal CreateProposalInput CreateParliamentProposalInput(IMessage input, Address organizationAddress)
    {
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = nameof(ParliamentContractImplContainer.ParliamentContractImplStub
                .ChangeOrganizationProposerWhiteList),
            ToAddress = ParliamentAddress,
            Params = input.ToByteString(),
            ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
            OrganizationAddress = organizationAddress
        };
        return createProposalInput;
    }
}