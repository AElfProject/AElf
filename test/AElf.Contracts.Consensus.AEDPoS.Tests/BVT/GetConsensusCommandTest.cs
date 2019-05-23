using System.Linq;
using System.Threading.Tasks;
using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        /// <summary>
        /// For now the information of first round will be filled in first block,
        /// which means this information should exist before mining process starting.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CheckFirstRound()
        {
            var firstRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            firstRound.RoundNumber.ShouldBe(1);
            firstRound.RealTimeMinersInformation.Count.ShouldBe(AEDPoSContractConstants.InitialMinersCount);
            firstRound.GetMiningInterval().ShouldBe(AEDPoSContractConstants.MiningInterval);
        }

        [Fact]
        public async Task First_Round_Test()
        {
            // In first round, boot node will get a consensus command of UpdateValueWithoutPreviousInValue behaviour
            {
                var command = await BootMiner.GetConsensusCommand.CallAsync(new BytesValue
                    {Value = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(AEDPoSContractConstants.MiningInterval);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
                command.Hint.ShouldBe(new AElfConsensusHint
                        {Behaviour = AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue}
                    .ToByteArray());
            }

            // Other miners will get a consensus command of NextRound behaviour
            {
                var otherMinerKeyPair = InitialMinersKeyPairs[1];
                var otherMiner = GetAEDPoSContractStub(otherMinerKeyPair);
                var round = await otherMiner.GetCurrentRoundInformation.CallAsync(new Empty());
                var order = round.RealTimeMinersInformation[otherMinerKeyPair.PublicKey.ToHex()].Order;
                var command = await otherMiner.GetConsensusCommand.CallAsync(new BytesValue
                    {Value = ByteString.CopyFrom(otherMinerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(
                    AEDPoSContractConstants.MiningInterval * AEDPoSContractConstants.InitialMinersCount +
                    AEDPoSContractConstants.MiningInterval * order);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
                command.Hint.ShouldBe(new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                    .ToByteArray());
            }
        }

        [Fact]
        public async Task Normal_Round_First_Miner_Test()
        {
            await BootMinerChangeRoundAsync();

            // Check second round information.
            var secondRound = await BootMiner.GetCurrentRoundInformation.CallAsync(new Empty());
            secondRound.RoundNumber.ShouldBe(2);

            var firstMinerInSecondRound =
                secondRound.RealTimeMinersInformation.Values.First(m => m.Order == 1).PublicKey;

            var minerKeyPair = InitialMinersKeyPairs.First(k => k.PublicKey.ToHex() == firstMinerInSecondRound);
            var miner = GetAEDPoSContractStub(minerKeyPair);

            var expectedMiningTime = secondRound.RealTimeMinersInformation[minerKeyPair.PublicKey.ToHex()]
                .ExpectedMiningTime.ToDateTime();

            // Normal block
            {
                // Set current time as the start time of 2rd round.
                BlockTimeProvider.SetBlockTime(secondRound.GetStartTime());

                var leftMilliseconds = (int) (expectedMiningTime - secondRound.GetStartTime()).TotalMilliseconds;

                var command = await miner.GetConsensusCommand.CallAsync(new BytesValue
                    {Value = ByteString.CopyFrom(minerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(leftMilliseconds);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
                command.Hint.ShouldBe(new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.UpdateValue}
                    .ToByteArray());
            }

            // Extra block
            {
                // Pretend the miner passed his time slot.
                var fakeTime = expectedMiningTime.AddMilliseconds(AEDPoSContractConstants.MiningInterval);
                BlockTimeProvider.SetBlockTime(fakeTime);

                var extraBlockMiningTime = secondRound.GetExpectedEndTime().ToDateTime()
                    .AddMilliseconds(AEDPoSContractConstants.MiningInterval);
                var leftMilliseconds = (int) (extraBlockMiningTime - fakeTime).TotalMilliseconds;

                var command = await miner.GetConsensusCommand.CallAsync(new BytesValue
                    {Value = ByteString.CopyFrom(minerKeyPair.PublicKey)});
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

                command.LimitMillisecondsOfMiningBlock.ShouldBe(AEDPoSContractConstants.SmallBlockMiningInterval);
                command.Hint.ShouldBe(new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.NextRound}
                    .ToByteArray());
            }
        }
    }
}