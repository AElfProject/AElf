using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

// ReSharper disable once InconsistentNaming
public class AEDPoSExtensionTests : AEDPoSExtensionDemoTestBase
{
    [Fact]
    public async Task Demo_Test()
    {
        InitialContracts();
        InitialAcs3Stubs();

        // Check round information after initialization.
        {
            var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.RoundNumber.ShouldBe(1);
            round.TermNumber.ShouldBe(1);
            round.RealTimeMinersInformation.Count.ShouldBe(AEDPoSExtensionConstants.InitialKeyPairCount);

            TestDataProvider.SetBlockTime(
                round.RealTimeMinersInformation.Single(m => m.Value.Order == 1).Value.ExpectedMiningTime +
                new Duration { Seconds = 1 });
        }

        // We can use this method process testing.
        // Basically this will produce one block with no transaction.
        await BlockMiningService.MineBlockAsync();

        // And this will produce one block with one transaction.
        // This transaction will call Create method of Token Contract.
        await ParliamentStubs.First().Initialize.SendAsync(new InitializeInput
        {
            ProposerAuthorityRequired = false,
            PrivilegedProposer = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.First().PublicKey)
        });
        var defaultOrganizationAddress =
            await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
        await ParliamentReachAnAgreementAsync(new CreateProposalInput
        {
            ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
            ContractMethodName = nameof(TokenStub.Create),
            Params = new CreateInput
            {
                Symbol = "ELF",
                Decimals = 8,
                TokenName = "Test",
                Issuer = Accounts[0].Address,
                IsBurnable = true,
                TotalSupply = 1_000_000_000_00000000
            }.ToByteString(),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = defaultOrganizationAddress
        });
        
        const long issueTokenAmount = 10_0000_00000000;
        var issueToAddress = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.First().PublicKey);
        var issueTokenTransaction = TokenStub.Issue.GetTransaction(new IssueInput
        {
            Symbol = "ELF",
            Amount = issueTokenAmount,
            To = issueToAddress
        });
        await BlockMiningService.MineBlockAsync(new List<Transaction>
        {
            issueTokenTransaction
        });

        var createTokenTransactionTrace =
            TransactionTraceProvider.GetTransactionTrace(issueTokenTransaction.GetHash());
        createTokenTransactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);

        // Check whether previous Create transaction successfully executed.
        {
            var tokenInfo = await TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = "ELF" });
            tokenInfo.Symbol.ShouldBe("ELF");
        }

        for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber; i++) await BlockMiningService.MineBlockAsync();

        var getBalanceTransaction = TokenStub.GetBalance.GetTransaction(new GetBalanceInput
        {
            Owner = issueToAddress,
            Symbol = "ELF"
        });
        // Miner of order 2 produce his first block.
        await BlockMiningService.MineBlockAsync(new List<Transaction> { getBalanceTransaction });

        var getBalanceTransactionTrace =
            TransactionTraceProvider.GetTransactionTrace(getBalanceTransaction.GetHash());
        getBalanceTransactionTrace.ReturnValue.ShouldNotBeNull();
        var output = GetBalanceOutput.Parser.ParseFrom(getBalanceTransactionTrace.ReturnValue);
        output.Balance.ShouldBe(issueTokenAmount);

        // Next steps will check whether the AEDPoS process is correct.
        // Now 2 miners produced block during first round, so there should be 2 miners' OutValue isn't null.
        {
            var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(2);
        }

        for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber - 1; i++)
            await BlockMiningService.MineBlockAsync();

        // Miner of order 3 produce his first block.
        {
            await BlockMiningService.MineBlockAsync();
            var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(3);
        }

        for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber - 1; i++)
            await BlockMiningService.MineBlockAsync();

        // Currently we have 5 miners, and before this line, 3 miners already produced blocks.
        // 3 more blocks will end current round.
        for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber * 3; i++)
            await BlockMiningService.MineBlockAsync(new List<Transaction>());

        // Check round number.
        {
            var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
            if (round.RoundNumber != 2) throw new Exception(round.ToString());

            round.RoundNumber.ShouldBe(2);
        }

        var countDown = await ConsensusStub.GetNextElectCountDown.CallAsync(new Empty());
        countDown.Value.ShouldBePositive();

        // 5 more blocks will end second round.
        for (var i = 0; i < AEDPoSExtensionConstants.TinyBlocksNumber * 6; i++)
            await BlockMiningService.MineBlockAsync(new List<Transaction>());

        // Check round number.
        {
            var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
            round.RoundNumber.ShouldBe(3);
        }

        var randomHash = await ConsensusStub.GetRandomHash.CallAsync(new Int64Value { Value = 100 });
        randomHash.ShouldNotBe(Hash.Empty);
    }

    [Fact]
    public async Task NotMinerTest()
    {
        InitialContracts();
        var keyPair = SampleAccount.Accounts.Last().KeyPair;
        var stub = GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
            ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
            keyPair);
        var command = await stub.GetConsensusCommand.CallAsync(new BytesValue
            { Value = ByteString.CopyFrom(keyPair.PublicKey) });
        command.Hint.ShouldBe(ByteString.CopyFrom(new AElfConsensusHint
        {
            Behaviour = AElfConsensusBehaviour.Nothing
        }.ToByteArray()));
    }
}