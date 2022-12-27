using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests;

public class ExecutionPluginForMethodFeeTestBase : ContractTestBase<ExecutionPluginForMethodFeeTestModule>
{
}

public class ExecutionPluginForMethodFeeWithForkTestBase : Contracts.TestBase.ContractTestBase<
    ExecutionPluginForMethodFeeWithForkTestModule>
{
    private readonly Address _parliamentAddress;
    protected readonly Address TokenContractAddress;

    protected ExecutionPluginForMethodFeeWithForkTestBase()
    {
        AsyncHelper.RunSync(() =>
            Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _, out _,
                out _)));
        TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        _parliamentAddress = Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
        var amount = 1000_00000000;
        AsyncHelper.RunSync(() =>
            IssueNativeTokenAsync(Address.FromPublicKey(Tester.InitialMinerList[0].PublicKey), amount));
        AsyncHelper.RunSync(() =>
            IssueNativeTokenAsync(Address.FromPublicKey(Tester.InitialMinerList[2].PublicKey), amount));
    }

    private async Task IssueNativeTokenAsync(Address address, long amount)
    {
        await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
            nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
            {
                Amount = amount,
                Memo = Guid.NewGuid().ToString(),
                Symbol = "ELF",
                To = address
            });
    }

    protected async Task SetMethodFeeWithProposalAsync(ByteString methodFee)
    {
        var proposal = await Tester.ExecuteContractWithMiningAsync(_parliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateProposal),
            new CreateProposalInput
            {
                ContractMethodName =
                    nameof(MethodFeeProviderContractContainer.MethodFeeProviderContractStub.SetMethodFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = methodFee,
                ToAddress = TokenContractAddress,
                OrganizationAddress = await GetParliamentDefaultOrganizationAddressAsync()
            });
        var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
        await ApproveWithMinersAsync(proposalId);
        await ReleaseProposalAsync(proposalId);
    }

    private async Task<Address> GetParliamentDefaultOrganizationAddressAsync()
    {
        var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                _parliamentAddress,
                nameof(ParliamentContractImplContainer.ParliamentContractImplStub.GetDefaultOrganizationAddress),
                new Empty()))
            .ReturnValue);
        return organizationAddress;
    }

    private async Task ApproveWithMinersAsync(Hash proposalId)
    {
        var approveTransaction1 = await GenerateTransactionAsync(_parliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), Tester.InitialMinerList[0],
            proposalId);
        var approveTransaction2 = await GenerateTransactionAsync(_parliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), Tester.InitialMinerList[1],
            proposalId);
        var approveTransaction3 = await GenerateTransactionAsync(_parliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Approve), Tester.InitialMinerList[2],
            proposalId);

        // Mine a block with given normal txs and system txs.
        await Tester.MineAsync(
            new List<Transaction> { approveTransaction1, approveTransaction2, approveTransaction3 });
    }

    private async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
        ECKeyPair ecKeyPair, IMessage input)
    {
        return ecKeyPair == null
            ? await Tester.GenerateTransactionAsync(contractAddress, methodName, input)
            : await Tester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
    }

    private async Task ReleaseProposalAsync(Hash proposalId)
    {
        await Tester.ExecuteContractWithMiningAsync(_parliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.Release), proposalId);
    }
}

public class ExecutePluginTransactionDirectlyForMethodFeeTestBase : ContractTestBase<
    ExecutionPluginTransactionDirectlyForMethodFeeTestModule>
{
    protected const string NativeTokenSymbol = "ELF";

    protected ExecutePluginTransactionDirectlyForMethodFeeTestBase()
    {
        AsyncHelper.RunSync(InitializeContracts);
    }

    internal Address TokenContractAddress { get; set; }
    internal Address TreasuryContractAddress { get; set; }
    internal Address ConsensusContractAddress { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub2 { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub3 { get; set; }
    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }
    internal ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    internal ECKeyPair DelegateeKeyPair => Accounts[1].KeyPair;
    internal ECKeyPair UserKeyPair => Accounts[2].KeyPair;
    internal ECKeyPair UserAKeyPair => Accounts[3].KeyPair;

    internal ECKeyPair UserTomSenderKeyPair => Accounts[10].KeyPair;
    internal Address UserTomSender => Accounts[10].Address;
    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(1).Select(a => a.KeyPair).ToList();

    internal Address DefaultSender => Accounts[0].Address;
    internal Address delegateeAddress => Accounts[1].Address;
    internal Address userAddress => Accounts[2].Address;

    internal TokenContractContainer.TokenContractStub TokenContractStubA { get; set; }
    internal Address UserAAddress => Accounts[3].Address;
    internal Address UserCAddress => Accounts[4].Address;

 
    private async Task InitializeContracts()
    {
        await DeployContractsAsync();
        await InitializeAElfConsensus();
        await InitializedParliament();
        await CreateNativeTokenAsync();
    }

    private async Task InitializedParliament()
    {
        await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            ProposerAuthorityRequired = false,
            PrivilegedProposer = DefaultSender
        });
    }

    private async Task DeployContractsAsync()
    {
        const int category = KernelConstants.CodeCoverageRunnerCategory;
        // Token contract
        {
            var code = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
            TokenContractAddress = await DeploySystemSmartContract(category, code,
                TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            TokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
            TokenContractStub2 =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DelegateeKeyPair);
            TokenContractStub3 = 
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, UserKeyPair);
            TokenContractStubA = 
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, UserKeyPair);
        }

        // Parliament
        {
            var code = Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
            var parliamentContractAddress = await DeploySystemSmartContract(category, code,
                ParliamentSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            ParliamentContractStub =
                GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(parliamentContractAddress,
                    DefaultSenderKeyPair);
        }

        //Consensus
        {
            var code = Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
            ConsensusContractAddress = await DeploySystemSmartContract(category, code,
                HashHelper.ComputeFrom("AElf.ContractNames.Consensus"), DefaultSenderKeyPair);
            AEDPoSContractStub =
                GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress,
                    DefaultSenderKeyPair);
        }

        // Treasury
        {
            var code = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
            TreasuryContractAddress = await DeploySystemSmartContract(category, code,
                HashHelper.ComputeFrom("Treasury"), DefaultSenderKeyPair);
        }
    }

    private async Task CreateNativeTokenAsync()
    {
        const long totalSupply = 1_000_000_000_00000000;
        //init elf token
        var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = NativeTokenSymbol,
            Decimals = 8,
            IsBurnable = true,
            TokenName = "elf token",
            TotalSupply = totalSupply,
            Issuer = DefaultSender
        });
        createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    protected async Task SetPrimaryTokenSymbolAsync()
    {
        await TokenContractStub.SetPrimaryTokenSymbol.SendAsync(new SetPrimaryTokenSymbolInput
            { Symbol = NativeTokenSymbol });
    }

    private async Task InitializeAElfConsensus()
    {
        {
            await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                new InitialAElfConsensusContractInput
                {
                    PeriodSeconds = 604800L,
                    MinerIncreaseInterval = 31536000
                });
        }
        {
            await AEDPoSContractStub.FirstRound.SendAsync(
                GenerateFirstRoundOfNewTerm(
                    new MinerList
                        { Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) } },
                    4000, TimestampHelper.GetUtcNow()));
        }
    }

    private Round GenerateFirstRoundOfNewTerm(MinerList minerList, int miningInterval,
        Timestamp currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
    {
        var sortedMiners = minerList.Pubkeys.Select(x => x.ToHex()).ToList();
        var round = new Round();

        for (var i = 0; i < sortedMiners.Count; i++)
        {
            var minerInRound = new MinerInRound();

            // The third miner will be the extra block producer of first round of each term.
            if (i == 0) minerInRound.IsExtraBlockProducer = true;

            minerInRound.Pubkey = sortedMiners[i];
            minerInRound.Order = i + 1;
            minerInRound.ExpectedMiningTime = currentBlockTime.AddMilliseconds(i * miningInterval + miningInterval);
            // Should be careful during validation.
            minerInRound.PreviousInValue = Hash.Empty;
            round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
        }

        round.RoundNumber = currentRoundNumber + 1;
        round.TermNumber = currentTermNumber + 1;
        round.IsMinerListJustChanged = true;
        round.ExtraBlockProducerOfPreviousRound = sortedMiners[0];

        return round;
    }
}