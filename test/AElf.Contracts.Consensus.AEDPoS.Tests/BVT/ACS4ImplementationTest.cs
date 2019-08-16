using System.Linq;
using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.Economic.TestBase;
using AElf.Kernel;
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
        internal async Task<ConsensusCommand> AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner_Test()
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
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_BootMiner_Test()
        {
            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner_Test();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            });

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
            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_BootMiner_Test();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            });

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.UpdateValue));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_BootMiner_Test()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_BootMiner()).Transactions.First();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            });

            var updateValueInput = new UpdateValueInput();
            updateValueInput.MergeFrom(transaction.Params);

            await AEDPoSContractStub.UpdateValue.SendAsync(updateValueInput);

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RoundNumber.ShouldBe(1);
            currentRound.RealTimeMinersInformation[BootMinerKeyPair.PublicKey.ToHex()].OutValue.ShouldNotBeNull();
        }

        [Fact]
        internal async Task<ConsensusCommand> AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner_Test()
        {
            await AEDPoSContract_FirstRound_BootMiner_Test();
            // Now the first time slot of first round already filled by boot miner.

            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Div(1000)
            });

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(AEDPoSContractTestConstants.MiningInterval
                .Mul(2));
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
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_SecondMiner_Test()
        {
            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner_Test();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            });

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
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_SecondMiner_Test()
        {
            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_SecondMiner_Test();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            });

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.UpdateValue));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_SecondMiner_Test()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_SecondMiner_Test()).Transactions.First();

            var usingKeyPair = InitialCoreDataCenterKeyPairs[1];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            });

            var updateValueInput = new UpdateValueInput();
            updateValueInput.MergeFrom(transaction.Params);

            var stub = GetAEDPoSContractStub(usingKeyPair);
            await stub.UpdateValue.SendAsync(updateValueInput);

            var currentRound = await stub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RoundNumber.ShouldBe(1);
            currentRound.RealTimeMinersInformation[usingKeyPair.PublicKey.ToHex()].OutValue.ShouldNotBeNull();
        }

        [Fact]
        internal async Task<ConsensusCommand> AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test()
        {
            await AEDPoSContract_FirstRound_SecondMiner_Test();

            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(2).Div(1000)
            });

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);

            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(
                AEDPoSContractTestConstants.MiningInterval.Mul(
                    EconomicContractsTestConstants.InitialCoreDataCenterCount));
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractTestConstants
                .SmallBlockMiningInterval);
            var hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                .ToByteString();
            consensusCommand.Hint.ShouldBe(hint);

            return consensusCommand;
        }

        [Fact]
        public async Task AEDPoSContract_GetInformationToUpdateConsensus_FirstRound_ExtraBlockMiner_Test()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(AEDPoSContractTestConstants.InitialMinersCount)
                    .Div(1000)
            });

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());

            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var extraData = extraDataBytes.ToConsensusHeaderInformation();

            extraData.Round.RoundId.ShouldNotBe(0);
            extraData.Round.RoundNumber.ShouldBe(2);
        }

        [Fact]
        internal async Task<TransactionList> AEDPoSContract_GenerateConsensusTransactions_FirstRound_ExtraBlockMiner_Test()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(AEDPoSContractTestConstants.InitialMinersCount)
                    .Div(1000)
            });

            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

            var transactionList = await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

            transactionList.Transactions.Count.ShouldBe(1);
            transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.NextRound));

            return transactionList;
        }

        [Fact]
        public async Task AEDPoSContract_FirstRound_Terminate_Test()
        {
            var transaction =
                (await AEDPoSContract_GenerateConsensusTransactions_FirstRound_ExtraBlockMiner_Test()).Transactions.First();

            BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
            {
                Seconds = AEDPoSContractTestConstants.MiningInterval.Mul(AEDPoSContractTestConstants.InitialMinersCount)
                    .Div(1000)
            });

            var nextRound = new Round();
            nextRound.MergeFrom(transaction.Params);

            await AEDPoSContractStub.NextRound.SendAsync(nextRound);

            var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            currentRound.RoundNumber.ShouldBe(2);
        }

        [Fact]
        public async Task AEDPoSContract_ConsensusTransactionValidation_Test()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test();
            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());
            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);

            var validateBeforeResult =
                await AEDPoSContractStub.ValidateConsensusBeforeExecution.CallAsync(extraDataBytes);
            validateBeforeResult.Success.ShouldBeTrue();

            var roundInfo = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            roundInfo.RoundNumber++;
            var transactionResult = await AEDPoSContractStub.NextRound.SendAsync(roundInfo);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var validateAfterResult =
                await AEDPoSContractStub.ValidateConsensusAfterExecution.CallAsync(roundInfo.ToBytesValue());
            validateAfterResult.Success.ShouldBeFalse(); //update with extra data would be keep the same.
        }

        [Fact]
        public async Task AEDPoSContract_ValidateConsensusBeforeExecution_UpdateValue_WithoutMiner_Test()
        {
            var usingKeyPair = ValidationDataCenterKeyPairs[0];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test();
            var updateValue = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValue}
                .ToByteString();
            consensusCommand.Hint = updateValue;
            
            var triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());
            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);
            
            await NextTerm(BootMinerKeyPair);
            
            var otherUser = GetAEDPoSContractStub(usingKeyPair);
            var validateBeforeResult =
                await otherUser.ValidateConsensusBeforeExecution.CallAsync(extraDataBytes);
            validateBeforeResult.Success.ShouldBeFalse();
            validateBeforeResult.Message.ShouldContain("is not a miner");
        }
        
        [Fact]
        public async Task AEDPoSContract_ValidateConsensusBeforeExecution_UpdateValue_Test()
        {
            var usingKeyPair = InitialCoreDataCenterKeyPairs[0];
            KeyPairProvider.SetKeyPair(usingKeyPair);

            var triggerForCommand =
                TriggerInformationProvider.GetTriggerInformationForConsensusCommand(new BytesValue());
            var consensusCommand = await AEDPoSContractStub.GetConsensusCommand.CallAsync(triggerForCommand);
            var updateValue = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValue}
                .ToByteString();
            consensusCommand.Hint = updateValue;
            
            triggerForCommand =
                await TriggerInformationProvider
                    .GetTriggerInformationForBlockHeaderExtraDataAsync(consensusCommand.ToBytesValue());
            var extraDataBytes = await AEDPoSContractStub.GetInformationToUpdateConsensus.CallAsync(triggerForCommand);
            
            var validateBeforeResult =
                await AEDPoSContractStub.ValidateConsensusBeforeExecution.CallAsync(extraDataBytes);
            validateBeforeResult.Success.ShouldBeTrue();
        }

        [Fact]
        public async Task AEDPoSContract_GenerateConsensusTransaction_TinyBlock_Test()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            //Tiny block
            {
                var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test();
                var tinyBlockBehavior = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.TinyBlock}
                    .ToByteString();
                consensusCommand.Hint = tinyBlockBehavior;
                BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
                {
                    Seconds = AEDPoSContractTestConstants.MiningInterval
                        .Mul(AEDPoSContractTestConstants.InitialMinersCount)
                        .Div(1000)
                });

                var triggerForCommand =
                    await TriggerInformationProvider
                        .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

                var transactionList =
                    await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

                transactionList.Transactions.Count.ShouldBe(1);
                transactionList.Transactions[0].MethodName
                    .ShouldBe(nameof(AEDPoSContractStub.UpdateTinyBlockInformation));
            }
        }

        [Fact]
        public async Task AEDPoSContract_GenerateConsensusTransaction_NextTerm_Test()
        {
            var usingKeyPair = BootMinerKeyPair;
            KeyPairProvider.SetKeyPair(usingKeyPair);

            //Next term
            {
                var consensusCommand = await AEDPoSContract_GetConsensusCommand_FirstRound_ExtraBlockMiner_Test();
                var nextTermBehavior = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextTerm}
                    .ToByteString();
                consensusCommand.Hint = nextTermBehavior;
                BlockTimeProvider.SetBlockTime(BlockchainStartTimestamp + new Duration
                {
                    Seconds = AEDPoSContractTestConstants.MiningInterval
                        .Mul(AEDPoSContractTestConstants.InitialMinersCount)
                        .Div(1000)
                });

                var triggerForCommand =
                    await TriggerInformationProvider
                        .GetTriggerInformationForConsensusTransactionsAsync(consensusCommand.ToBytesValue());

                var transactionList =
                    await AEDPoSContractStub.GenerateConsensusTransactions.CallAsync(triggerForCommand);

                transactionList.Transactions.Count.ShouldBe(1);
                transactionList.Transactions[0].MethodName.ShouldBe(nameof(AEDPoSContractStub.NextTerm));
            }
        }
    }
}