using System;
using System.Linq;
using Acs4;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS
{
    public class BasicExtensionsTest
    {
        /// <summary>
        /// Really basic tests about time stuff.
        /// </summary>
        [Fact]
        public void TestsAboutTime()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow;

            var round = GenerateFirstRound(startTimestamp, minersCount, miningInterval);

            round.GetMiningInterval().ShouldBe(miningInterval);

            round.TotalMilliseconds().ShouldBe(miningInterval * minersCount + miningInterval);

            round.GetStartTime().ShouldBe(startTimestamp);

            round.GetExpectedEndTime()
                .ShouldBe(startTimestamp.AddMilliseconds(minersCount * miningInterval + miningInterval).ToTimestamp());
        }

        [Fact]
        public void GetExpectedMiningTimeTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow;

            var round = GenerateFirstRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            round.GetExpectedMiningTime(firstMiner.PublicKey).ShouldBe(firstMiner.ExpectedMiningTime);
        }

        [Fact]
        public void IsTimeSlotPassed_Yes()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow;

            var round = GenerateFirstRound(startTimestamp, minersCount, miningInterval);

            var firstMinerPublicKey = round.RealTimeMinersInformation.Keys.First();

            // Entered his time slot but not total passed.
            {
                var testTimestamp = startTimestamp.AddMilliseconds(1);
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTimestamp, out _);

                isTimeSlotPassed.ShouldBe(false);
            }

            // Time slot passed but still in current round.
            {
                var testTime = startTimestamp.AddMilliseconds(2 * miningInterval);
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTime, out _);

                isTimeSlotPassed.ShouldBe(true);
            }

            // Time slot passed and beyond current round.
            {
                var testTimestamp = startTimestamp.AddMilliseconds(round.TotalMilliseconds());
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTimestamp, out _);

                isTimeSlotPassed.ShouldBe(true);
            }
        }

        [Fact]
        public void IsTimeSlotPassed_No()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();
            var testTime = startTime.AddMilliseconds(-1);
            var isTimeSlotPassed = round.IsTimeSlotPassed(firstMiner.PublicKey, testTime, out _);

            isTimeSlotPassed.ShouldBe(false);
        }

        [Fact]
        public void ArrangeAbnormalMiningTimeTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;
            
            var round = GenerateFirstRound(startTime, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            // If this node noticed he missed his time slot several rounds later.
            {
                const int missedRoundsCount = 10;
                var testTime = startTime.AddMilliseconds(round.TotalMilliseconds() * missedRoundsCount);
                var arrangedMiningTime = round.ArrangeAbnormalMiningTime(firstMiner.PublicKey, testTime);

                arrangedMiningTime.ShouldBe(round.GetExpectedEndTime(missedRoundsCount).ToDateTime()
                    .AddMilliseconds(miningInterval * firstMiner.Order).ToTimestamp());
            }
        }

        [Fact]
        public void GenerateNextRoundInformation()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            var actualMiningTime = startTime.AddMilliseconds(1);
            var publicKey = firstMiner.PublicKey;
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            var roundAfter =
                round.ApplyNormalConsensusData(publicKey, inValue, outValue, Hash.Empty, actualMiningTime);

            var terminateTime = round.GetExpectedEndTime().ToDateTime().AddMilliseconds(1);

            var result = roundAfter.GenerateNextRoundInformation(terminateTime, startTime.ToTimestamp(), out var secondRound);

            Assert.True(result);
            Assert.Equal(2L, secondRound.RoundNumber);
            Assert.Equal(minersCount, secondRound.RealTimeMinersInformation.Count);
            Assert.Equal(1, secondRound.RealTimeMinersInformation.Values.Count(m => m.IsExtraBlockProducer));
        }

        [Fact]
        public void IsTimeToChangeTermTest_CannotChange()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            var actualMiningTime = startTime.AddMilliseconds(1);
            var publicKey = firstMiner.PublicKey;
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            round.ApplyNormalConsensusData(publicKey, inValue, outValue, Hash.Empty, actualMiningTime);

            var terminateTime = round.GetExpectedEndTime().ToDateTime().AddMilliseconds(1);

            round.GenerateNextRoundInformation(terminateTime, startTime.ToTimestamp(), out var secondRound);

            var result = secondRound.IsTimeToChangeTerm(round, startTime, 1);

            Assert.False(result);
        }

        [Fact]
        public void IsTimeToChangeTermTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;
            
            var round = GenerateFirstRound(startTime, minersCount, miningInterval);

            foreach (var minerInRound in round.RealTimeMinersInformation)
            {
                round.ApplyNormalConsensusData(minerInRound.Key, Hash.Generate(),Hash.Generate(), Hash.Empty,
                    startTime.AddMinutes(ConsensusDPoSConsts.DaysEachTerm + 1));
            }

            var result = round.IsTimeToChangeTerm(round, startTime, 1);

            Assert.True(result);
        }

        [Fact]
        public void ApplyNormalConsensusDataTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            var actualMiningTime = startTime.AddMilliseconds(1);
            var publicKey = firstMiner.PublicKey;
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            var roundAfter =
                round.ApplyNormalConsensusData(publicKey, inValue, outValue, Hash.Empty, actualMiningTime);

            var minerInRoundAfter = roundAfter.RealTimeMinersInformation[publicKey];

            Assert.Equal(actualMiningTime, minerInRoundAfter.ActualMiningTime.ToDateTime());
            Assert.Equal(publicKey, minerInRoundAfter.PublicKey);
            Assert.Equal(outValue, minerInRoundAfter.OutValue);
        }

        [Fact]
        public void GetConsensusCommand_Nothing()
        {

            var behaviour = DPoSBehaviour.Nothing;
            var miningInterval = 4000;
            var round = GenerateFirstRound(DateTime.UtcNow, 3, miningInterval);
            var consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.First().Key, 
                DateTime.UtcNow, true);
            
            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(int.MaxValue);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(int.MaxValue);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.Nothing);
        }

        [Fact]
        public void GetConsensusCommand_NextTerm()
        {
            var behaviour = DPoSBehaviour.NextTerm;
            var miningInterval = 4000;
            var round = GenerateFirstRound(DateTime.UtcNow, 3, miningInterval);
            round.RealTimeMinersInformation.Last().Value.PromisedTinyBlocks = 1; //default is 0

            var consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.Last().Key,
                DateTime.UtcNow, true);
            
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(miningInterval);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.NextTerm);
        }
        
        [Fact]
        public void GetConsensusCommand_NextRound()
        {
            var behaviour = DPoSBehaviour.NextRound;
            var miningInterval = 4000;
            var round = GenerateFirstRound(DateTime.UtcNow, 3, miningInterval);
            round.RealTimeMinersInformation.Last().Value.PromisedTinyBlocks = 1; //default is 0

            var consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.Last().Key,
                DateTime.UtcNow, true);
            
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(miningInterval);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.NextRound);
        }
        
        [Fact]
        public void GetConsensusCommand_UpdateValue()
        {
            var behaviour = DPoSBehaviour.UpdateValue;
            var miningInterval = 4000;
            var round = GenerateFirstRound(DateTime.UtcNow, 3, miningInterval);
            round.RealTimeMinersInformation.First(m=>m.Value.Order == 2).Value.PromisedTinyBlocks = 1; //default is 0
            
            var consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.First(m=>m.Value.Order == 2).Key,
                DateTime.UtcNow, false);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(miningInterval);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.UpdateValue);
            
            //skip time slot
            consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.First(m=>m.Value.Order == 2).Key,
                            DateTime.UtcNow, true);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(miningInterval);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.NextRound);
        }

        [Fact]
        public void GetConsensusCommand_UpdateValueWithoutPreviousInValue()
        {
            var behaviour = DPoSBehaviour.UpdateValueWithoutPreviousInValue;
            var miningInterval = 4000;
            var round = GenerateFirstRound(DateTime.UtcNow, 3, miningInterval);
            round.RealTimeMinersInformation.First(m=>m.Value.Order == 2).Value.PromisedTinyBlocks = 1; //default is 0
            
            var consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.First(m=>m.Value.Order == 2).Key,
                DateTime.UtcNow, false);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(miningInterval);
            consensusCommand.NextBlockMiningLeftMilliseconds.ShouldBe(2*miningInterval);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.UpdateValueWithoutPreviousInValue);
            
            //skip time slot
            consensusCommand = behaviour.GetConsensusCommand(round, round.RealTimeMinersInformation.First(m=>m.Value.Order == 2).Key,
                DateTime.UtcNow, true);
            consensusCommand.LimitMillisecondsOfMiningBlock.ShouldBe(miningInterval);
            DPoSHint.Parser.ParseFrom(consensusCommand.Hint.ToByteArray()).Behaviour.ShouldBe(DPoSBehaviour.NextRound);

        }

        [Fact]
        public void GetRoundHash()
        {
            var miningInterval = 4000;
            var round = GenerateFirstRound(DateTime.UtcNow, 3, miningInterval);

            var hash = round.GetHash();
            hash.ShouldNotBeNull();

            var hash1 = round.GetHash(false);
            hash1.ShouldNotBeNull();
        }
        
        /// <summary>
        /// Only able to generate information of first round.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="minersCount"></param>
        /// <param name="miningInterval"></param>
        /// <returns></returns>
        private Round GenerateFirstRound(DateTime startTime, int minersCount, int miningInterval)
        {
            var round = new Round {RoundNumber = 1};
            var extraBlockProducerOrder = new Random().Next(1, minersCount);
            for (var i = 0; i < minersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                var minerInRound = new MinerInRound
                {
                    PublicKey = keyPair.PublicKey.ToHex(),
                    Signature = Hash.Generate(),
                    Order = i + 1,
                    ExpectedMiningTime = startTime.AddMilliseconds(miningInterval * i).ToTimestamp()
                };

                if (extraBlockProducerOrder == minerInRound.Order)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                round.RealTimeMinersInformation.Add(minerInRound.PublicKey, minerInRound);
            }

            return round;
        }
    }
}