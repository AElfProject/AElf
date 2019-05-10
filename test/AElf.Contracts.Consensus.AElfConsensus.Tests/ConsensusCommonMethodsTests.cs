using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    public class ConsensusCommonMethodsTests : AElfConsensusContractTestBase
    {
        public ConsensusCommonMethodsTests()
        {
            InitializeContracts();
        }

        /// <summary>
        /// For now the information of first round will be filled in first block,
        /// which means this information should exist before mining process starting.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AElfConsensusContract_CheckFirstRound()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            firstRound.RoundNumber.ShouldBe(1);
            firstRound.RealTimeMinersInformation.Count.ShouldBe(InitialMinersCount);
            firstRound.GetMiningInterval().ShouldBe(MiningInterval);
        }

        [Fact]
        public async Task AElfConsensusContract_UpdateValueInFirstRound()
        {
            // In first round, boot node will get a consensus command of UpdateValueWithoutPreviousInValue behaviour
            {
                var command = await BootMiner.GetConsensusCommand.CallAsync(new CommandInput
                    {PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(MiningInterval);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(MiningInterval / AElfConsensusContractConstants.TinyBlocksNumber);
                command.Hint.ShouldBe(new AElfConsensusHint
                        {Behaviour = AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue}
                    .ToByteArray());
            }

            // Other miners will get a consensus command of NextRound behaviour
            {
                var otherMinerKeyPair = InitialMinersKeyPairs[1];
                var otherMiner = GetAElfConsensusContractTester(otherMinerKeyPair);
                var round = await otherMiner.GetCurrentRoundInformation.CallAsync(new Empty());
                var order = round.RealTimeMinersInformation[otherMinerKeyPair.PublicKey.ToHex()].Order;
                var command = await otherMiner.GetConsensusCommand.CallAsync(new CommandInput
                    {PublicKey = ByteString.CopyFrom(otherMinerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(
                    MiningInterval * InitialMinersCount + MiningInterval * order);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(MiningInterval / AElfConsensusContractConstants.TinyBlocksNumber);
                command.Hint.ShouldBe(new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                    .ToByteArray());
            }
        }


        [Fact(Skip = "Need more contracts to be deployed.")]
        public async Task AElfConsensusContract_Process()
        {
            await BootMinerChangeRoundAsync();

            // Check second round information.
            var secondRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            secondRound.RoundNumber.ShouldBe(2);

            var firstMinerInSecondRound =
                secondRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).PublicKey;

            var secondRoundStartTime = StartTimestamp.ToDateTime()
                .AddMilliseconds(secondRound.TotalMilliseconds(MiningInterval));

            var minerKeyPair = InitialMinersKeyPairs.First(k => k.PublicKey.ToHex() == firstMinerInSecondRound);
            var miner = GetAElfConsensusContractTester(minerKeyPair);

            var expectedMiningTime = secondRound.RealTimeMinersInformation[minerKeyPair.PublicKey.ToHex()]
                .ExpectedMiningTime.ToDateTime();

            // Normal block
            {
                // Set current time as the start time of 2rd round.
                BlockTimeProvider.SetBlockTime(secondRoundStartTime);

                var leftMilliseconds = (int) (expectedMiningTime - secondRoundStartTime).TotalMilliseconds;

                var command = await miner.GetConsensusCommand.CallAsync(new CommandInput
                    {PublicKey = ByteString.CopyFrom(minerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(leftMilliseconds);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(MiningInterval / AElfConsensusContractConstants.TinyBlocksNumber);
                command.Hint.ShouldBe(new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValue}
                    .ToByteArray());
            }

            // Extra block
            {
                // Pretend the miner passed his time slot.
                var fakeTime = expectedMiningTime.AddMilliseconds(MiningInterval);
                BlockTimeProvider.SetBlockTime(fakeTime);

                var extraBlockMiningTime =
                    secondRound.GetExpectedEndTime().ToDateTime().AddMilliseconds(MiningInterval);
                var leftMilliseconds = (int) (extraBlockMiningTime - fakeTime).TotalMilliseconds;

                var command = await miner.GetConsensusCommand.CallAsync(new CommandInput
                    {PublicKey = ByteString.CopyFrom(minerKeyPair.PublicKey)});
                if (secondRound.GetExtraBlockProducerInformation().PublicKey == minerKeyPair.PublicKey.ToHex())
                {
                    // If this node is EBP
                    command.NextBlockMiningLeftMilliseconds.ShouldBe(
                        (int) (secondRound.GetExtraBlockMiningTime() - fakeTime).TotalMilliseconds);
                }
                else
                {
                    command.NextBlockMiningLeftMilliseconds.ShouldBe(leftMilliseconds);
                }

                command.LimitMillisecondsOfMiningBlock.ShouldBe(MiningInterval / AElfConsensusContractConstants.TinyBlocksNumber);
                command.Hint.ShouldBe(new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                    .ToByteArray());
            }
        }
    }
}