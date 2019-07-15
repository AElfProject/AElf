using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.Economic.TestBase;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest : AEDPoSContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AEDPoSTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            InitializeContracts();
        }

        [Fact]
        internal async Task<ConsensusCommand> AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner()
        {
            KeyPairProvider.SetKeyPair(BootMinerKeyPair);
            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(EconomicContractsTestConstants.MiningInterval);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractTestConstants
                .SmallBlockMiningInterval);
            var hint = new AElfConsensusHint
            {
                Behaviour = AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue
            }.ToByteString();
            consensusCommand.Hint.ShouldBe(hint);

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_BootMiner()
        {
            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());

            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var extraData = extraDataBytes.ToConsensusHeaderInformation();

            extraData.Round.RoundId.ShouldNotBe(0);
            extraData.Round.RoundNumber.ShouldBe(1);
            extraData.Round.RealTimeMinersInformation.Count.ShouldBe(InitialCoreDataCenterKeyPairs.Count);
            extraData.Round.RealTimeMinersInformation[BootMinerKeyPair.PublicKey.ToHex()].OutValue
                .ShouldNotBeNull();
        }

        [Fact]
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_BootMiner()
        {
            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.UpdateValue));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_BootMiner()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_BootMiner()).Transactions.First();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            }).ToDateTime());

            var updateValueInput = new UpdateValueInput();
            updateValueInput.MergeFrom(transaction.Params);

            await AEDPoSContractStub.UpdateValue.SendAsync(updateValueInput);

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RoundNumber.ShouldBe(1);
            currentRound.RealTimeMinersInformation[BootMinerKeyPair.PublicKey.ToHex()].OutValue.ShouldNotBeNull();
        }

        [Fact]
        internal async Task<ConsensusCommand> AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner()
        {
            await AEDPoSContract_FirstRound_BootMiner();
            // Now the first time slot of first round already filled by boot miner.

            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(AEDPoSContractTestConstants.MiningInterval.Mul(2));
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractTestConstants
                .SmallBlockMiningInterval);
            var hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue}
                .ToByteString();
            consensusCommand.Hint.ShouldBe(hint);
            consensusCommand.ExpectedMiningTime.ShouldBe(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(3).Div(1000)
            });

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_SecondMiner()
        {
            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());

            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var extraData = extraDataBytes.ToConsensusHeaderInformation();

            extraData.Round.RoundId.ShouldNotBe(0);
            extraData.Round.RoundNumber.ShouldBe(1);
            extraData.Round.RealTimeMinersInformation[usingKeyPair.PublicKey.ToHex()].OutValue
                .ShouldNotBeNull();
        }

        [Fact]
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_SecondMiner()
        {
            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.UpdateValue));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_SecondMiner()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_SecondMiner()).Transactions.First();

            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var updateValueInput = new UpdateValueInput();
            updateValueInput.MergeFrom(transaction.Params);

            var stub = GetAEDPoSContractTester(usingKeyPair);
            await stub.UpdateValue.SendAsync(updateValueInput);

            var currentRound = await stub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RoundNumber.ShouldBe(1);
            currentRound.RealTimeMinersInformation[usingKeyPair.PublicKey.ToHex()].OutValue.ShouldNotBeNull();
        }

        [Fact]
        internal async Task<ConsensusCommand> AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner()
        {
            await AEDPoSContract_FirstRound_SecondMiner();

            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(
                AEDPoSContractTestConstants.MiningInterval.Mul(EconomicContractsTestConstants.InitialCoreDataCenterCount));
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractTestConstants
                .SmallBlockMiningInterval);
            var hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                .ToByteString();
            consensusCommand.Hint.ShouldBe(hint);

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_ExtraBlockMiner()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(AEDPoSContractTestConstants.InitialMinersCount)
                    .Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());

            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var extraData = extraDataBytes.ToConsensusHeaderInformation();

            extraData.Round.RoundId.ShouldNotBe(0);
            extraData.Round.RoundNumber.ShouldBe(2);
        }

        [Fact]
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_ExtraBlockMiner()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(AEDPoSContractTestConstants.InitialMinersCount)
                    .Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.NextRound));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_Terminate()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_ExtraBlockMiner()).Transactions.First();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(AEDPoSContractTestConstants.InitialMinersCount)
                    .Div(1000)
            }).ToDateTime());

            var nextRound = new Round();
            nextRound.MergeFrom(transaction.Params);

            await AEDPoSContractStub.NextRound.SendAsync(nextRound);

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RoundNumber.ShouldBe(2);
        }
        
        [Fact]
        public async Task AEDPoSContract_ConsensusTransactionValidation()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner();
            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());
            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var validateBeforeResult = await AEDPoSContractStub.ValidateConsensusBeforeExecution.CallAsync(extraDataBytes);
            validateBeforeResult.Success.ShouldBeTrue();

            var roundInfo = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            roundInfo.RoundNumber++;
            var transactionResult = await AEDPoSContractStub.NextRound.SendAsync(roundInfo);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var validateAfterResult = await AEDPoSContractStub.ValidateConsensusAfterExecution.CallAsync(roundInfo.ToBytesValue());
            validateAfterResult.Success .ShouldBeFalse(); //update with extra data would be keep the same.
        }
    }
}