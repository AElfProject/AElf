using System;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Cryptography;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.DPoS.SideChain
{
    public class BasicExtensionsTests : DPoSSideChainTestBase
    {
        /// <summary>
        /// Really basic tests about time stuff.
        /// </summary>
        [Fact]
        public void TestsAboutTime()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime.ToTimestamp(), minersCount, miningInterval);

            round.GetMiningInterval().ShouldBe(miningInterval);

            round.TotalMilliseconds().ShouldBe(miningInterval * minersCount + miningInterval);

            round.GetStartTime().ShouldBe(startTime);

            round.GetExpectedEndTime()
                .ShouldBe(startTime.AddMilliseconds(minersCount * miningInterval + miningInterval).ToTimestamp());
        }

        [Fact]
        public void GetExpectedMiningTimeTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateFirstRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            round.GetExpectedMiningTime(firstMiner.PublicKey).ShouldBe(firstMiner.ExpectedMiningTime);
        }

        [Fact]
        public void IsTimeSlotPassed_Yes()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime.ToTimestamp(), minersCount, miningInterval);

            var firstMinerPublicKey = round.RealTimeMinersInformation.Keys.First();

            // Entered his time slot but not total passed.
            {
                var testTime = startTime.AddMilliseconds(1);
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTime, out _);

                isTimeSlotPassed.ShouldBe(false);
            }

            // Time slot passed but still in current round.
            {
                var testTime = startTime.AddMilliseconds(2 * miningInterval);
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTime, out _);

                isTimeSlotPassed.ShouldBe(true);
            }

            // Time slot passed and beyond current round.
            {
                var testTime = startTime.AddMilliseconds(round.TotalMilliseconds());
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTime, out _);

                isTimeSlotPassed.ShouldBe(true);
            }
        }

        [Fact]
        public void IsTimeSlotPassed_No()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var round = GenerateFirstRound(startTime.ToTimestamp(), minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();
            var testTime = startTime.AddMilliseconds(-1);
            var isTimeSlotPassed = round.IsTimeSlotPassed(firstMiner.PublicKey, testTime, out _);

            isTimeSlotPassed.ShouldBe(false);
        }

        /// <summary>
        /// Notes: `ArrangeAbnormalMining` is a method only dealing with **abnormal** mining time arrangement.
        /// </summary>
        [Fact]
        public void ArrangeAbnormalMiningTimeTest()
        {
            const int minersCount = 3;
            const int miningInterval = 4000;

            var startTime = DateTime.UtcNow;

            var firstRound = GenerateFirstRound(startTime.ToTimestamp(), minersCount, miningInterval);

            var firstMiner = firstRound.RealTimeMinersInformation.Values.First();
            var secondMiner = firstRound.RealTimeMinersInformation.Values.First(m => m.Order == 2);

            // For first round, the boot miner's order will be 1.
            {
                // The really expected mining time will depends on miner's order, we don't care ExpectedMiningTime field.
                var fakeTime = DateTime.MinValue.ToUniversalTime();
                {
                    var arrangedMiningTimestamp = firstRound.ArrangeAbnormalMiningTime(firstMiner.PublicKey, fakeTime);

                    firstMiner.Order.ShouldBe(1);
                    arrangedMiningTimestamp.ShouldBe(fakeTime
                        .AddMilliseconds(firstRound.TotalMilliseconds() + miningInterval).ToTimestamp());
                }
                {
                    var arrangedMiningTimestamp = firstRound.ArrangeAbnormalMiningTime(secondMiner.PublicKey, fakeTime);

                    secondMiner.Order.ShouldBe(2);
                    arrangedMiningTimestamp.ShouldBe(fakeTime
                        .AddMilliseconds(firstRound.TotalMilliseconds() + miningInterval * 2).ToTimestamp());
                }
            }

            firstRound.GenerateNextRoundInformation(firstRound.GetExpectedEndTime().ToDateTime(), startTime.ToTimestamp(), out var secondRound);
            var secondRoundStartTime = firstRound.GetExpectedEndTime().ToDateTime().AddMilliseconds(miningInterval);
            
            // For second round
            {
                // If this node not missed his time slot. Will return 
                
                // If this node **just** missed his time slot. (not too much)
                {
                    const int offset = 1;
                    var testTime = secondRoundStartTime.AddMilliseconds(offset);
                    var arrangedMiningTimestamp = secondRound.ArrangeAbnormalMiningTime(firstMiner.PublicKey, testTime);

                    arrangedMiningTimestamp.ShouldBe(secondRound.GetExpectedMiningTime(firstMiner.PublicKey));
                }
            }
            


            // If this node noticed he missed his time slot several rounds later.
            {
                const int missedRoundsCount = 10;
                var testTime = secondRoundStartTime.AddMilliseconds(1 + secondRound.TotalMilliseconds() * missedRoundsCount);
                var arrangedMiningTime = secondRound.ArrangeAbnormalMiningTime(firstMiner.PublicKey, testTime);

                arrangedMiningTime.ShouldBe(secondRound.GetExpectedEndTime(missedRoundsCount).ToDateTime()
                    .AddMilliseconds(miningInterval * firstMiner.Order).ToTimestamp());
            }
        }

        [Fact]
        public void GenerateNextRoundInformation()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateFirstRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            var actualMiningTime = startTimestamp.ToDateTime().AddMilliseconds(1);
            var publicKey = firstMiner.PublicKey;
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            var roundAfter =
                round.ApplyNormalConsensusData(publicKey, inValue, outValue, Hash.Empty, actualMiningTime);

            var terminateTime = round.GetExpectedEndTime().ToDateTime().AddMilliseconds(1);

            var result = roundAfter.GenerateNextRoundInformation(terminateTime, startTimestamp, out var secondRound);

            Assert.True(result);
            Assert.Equal(2L, secondRound.RoundNumber);
            Assert.Equal(minersCount, secondRound.RealTimeMinersInformation.Count);
            Assert.Equal(1, secondRound.RealTimeMinersInformation.Values.Count(m => m.IsExtraBlockProducer));
        }

        [Fact]
        public void ApplyNormalConsensusDataTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateFirstRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            var actualMiningTime = startTimestamp.ToDateTime().AddMilliseconds(1);
            var publicKey = firstMiner.PublicKey;
            var inValue = Hash.Generate();
            var outValue = Hash.FromMessage(inValue);

            var roundAfter =
                round.ApplyNormalConsensusData(publicKey, inValue, outValue, Hash.Empty, actualMiningTime);

            var minerInRoundAfter = roundAfter.RealTimeMinersInformation[publicKey];

            Assert.Equal(actualMiningTime.ToTimestamp(), minerInRoundAfter.ActualMiningTime);
            Assert.Equal(publicKey, minerInRoundAfter.PublicKey);
            Assert.Equal(outValue, minerInRoundAfter.OutValue);
        }

        /// <summary>
        /// Only able to generate information of first round.
        /// </summary>
        /// <param name="startTimestamp"></param>
        /// <param name="minersCount"></param>
        /// <param name="miningInterval"></param>
        /// <returns></returns>
        private Round GenerateFirstRound(Timestamp startTimestamp, int minersCount, int miningInterval)
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
                    ExpectedMiningTime = startTimestamp.ToDateTime().AddMilliseconds(miningInterval * i).ToTimestamp()
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