using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    /// <summary>
    /// GetConsensusCommand method can only call. 
    /// </summary>
    public class GetConsensusCommandTest : DPoSTestBase
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
            firstRound.RealTimeMinersInformation.Count.ShouldBe(MinersCount);
            firstRound.GetMiningInterval().ShouldBe(MiningInterval);
        }

        [Fact]
        public async Task FirstRoundTest()
        {
            // In first round, boot node will get a consensus command of UpdateValueWithoutPreviousInValue behaviour
            {
                var command = await BootMiner.GetConsensusCommand.CallAsync(new CommandInput
                    {PublicKey = ByteString.CopyFrom(BootMinerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(MiningInterval);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(MiningInterval);
                command.Hint.ShouldBe(new DPoSHint {Behaviour = DPoSBehaviour.UpdateValueWithoutPreviousInValue}
                    .ToByteArray());
            }

            // Other miners will get a consensus command of NextRound behaviour
            {
                var otherMinerKeyPair = InitialMinersKeyPairs[1];
                var otherMiner = GetConsensusContractTester(otherMinerKeyPair);
                var round = await otherMiner.GetCurrentRoundInformation.CallAsync(new Empty());
                var order = round.RealTimeMinersInformation[otherMinerKeyPair.PublicKey.ToHex()].Order;
                var command = await otherMiner.GetConsensusCommand.CallAsync(new CommandInput
                    {PublicKey = ByteString.CopyFrom(otherMinerKeyPair.PublicKey)});
                command.NextBlockMiningLeftMilliseconds.ShouldBe(MiningInterval * MinersCount + MiningInterval * order);
                command.LimitMillisecondsOfMiningBlock.ShouldBe(MiningInterval);
                command.Hint.ShouldBe(new DPoSHint {Behaviour = DPoSBehaviour.NextRound}
                    .ToByteArray());
            }
        }

        [Fact]
        public async Task NormalTest()
        {
            await ChangeRound();

            var minerKeyPair = InitialMinersKeyPairs[1];
            var miner = GetConsensusContractTester(minerKeyPair);
            
            // Check second round information.
            var secondRound = await miner.GetCurrentRoundInformation.CallAsync(new Empty());
            secondRound.RoundNumber.ShouldBe(2);

            var expectedMiningTime = secondRound.RealTimeMinersInformation[minerKeyPair.PublicKey.ToHex()]
                .ExpectedMiningTime.ToDateTime();
            var roundStartTime =
                BlockchainStartTime.GetRoundExpectedStartTime(secondRound.TotalMilliseconds(MiningInterval), 2);
            var expectedLeftMilliseconds = (expectedMiningTime - roundStartTime).Milliseconds;

            var command = await miner.GetConsensusCommand.CallAsync(new CommandInput
                {PublicKey = ByteString.CopyFrom(minerKeyPair.PublicKey)});
            command.NextBlockMiningLeftMilliseconds.ShouldBe(expectedLeftMilliseconds);
            
        }
    }
}