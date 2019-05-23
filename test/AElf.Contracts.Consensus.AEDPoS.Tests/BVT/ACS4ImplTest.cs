using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest : AElfConsensusContractTestBase
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

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(AEDPoSContractConstants.MiningInterval);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
            var hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue}
                .ToByteString();
            consensusCommand.Hint.ShouldBe(hint);
            consensusCommand.ExpectedMiningTime.ShouldBe(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Div(1000)
            });

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_BootMiner()
        {
            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraData(consensusCommand.ToBytesValue());
            
            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var extraData = extraDataBytes.ToConsensusHeaderInformation();

            extraData.Round.RoundId.ShouldBeGreaterThan(1);
            extraData.Round.RoundNumber.ShouldBe(1);
            extraData.Round.RealTimeMinersInformation.Count.ShouldBe(InitialMiners.Count);
            extraData.Round.RealTimeMinersInformation[BootMinerKeyPair.PublicKey.ToHex()].OutValue
                .ShouldNotBeNull();
        }

        [Fact]
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_BootMiner()
        {
            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Div(1000)
            }).ToDateTime());
            
            var triggerForCommand =
                TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactions(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContract.UpdateValue));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_BootMiner()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_BootMiner()).Transactions.First();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Div(1000)
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

            var usingKeyPair = InitialMinersKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(AEDPoSContractConstants.MiningInterval);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
            var hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue}
                .ToByteString();
            consensusCommand.Hint.ShouldBe(hint);
            consensusCommand.ExpectedMiningTime.ShouldBe(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Mul(2).Div(1000)
            });

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_SecondMiner()
        {
            var usingKeyPair = InitialMinersKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner();
            
            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraData(consensusCommand.ToBytesValue());
            
            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var extraData = extraDataBytes.ToConsensusHeaderInformation();

            extraData.Round.RoundId.ShouldBeGreaterThan(1);
            extraData.Round.RoundNumber.ShouldBe(1);
            extraData.Round.RealTimeMinersInformation[usingKeyPair.PublicKey.ToHex()].OutValue
                .ShouldNotBeNull();
        }
        
        [Fact]
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_SecondMiner()
        {
            var usingKeyPair = InitialMinersKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner();

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactions(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContract.UpdateValue));

            return transactionList;
        }
        
        [Fact]
        public async Task AEDPoSContract_FirstRound_SecondMiner()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_SecondMiner()).Transactions.First();

            var usingKeyPair = InitialMinersKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime((BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var updateValueInput = new UpdateValueInput();
            updateValueInput.MergeFrom(transaction.Params);

            var stub = GetAEDPoSContractStub(usingKeyPair);
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
                Seconds = AEDPoSContractConstants.MiningInterval.Mul(2).Div(1000)
            }).ToDateTime());

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(
                AEDPoSContractConstants.MiningInterval.Mul(AEDPoSContractConstants.InitialMinersCount - 1));
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
            var hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                .ToByteString();
            consensusCommand.Hint.ShouldBe(hint);
            consensusCommand.ExpectedMiningTime.ShouldBe(currentRound.GetExtraBlockMiningTime().ToTimestamp());

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_ExtraBlockMiner()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner();
            
            
        }
    }
}