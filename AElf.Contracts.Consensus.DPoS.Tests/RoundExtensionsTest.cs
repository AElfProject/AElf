using System;
using System.Linq;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.Extensions;
using AElf.Cryptography;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    public class RoundExtensionsTest
    {
        /// <summary>
        /// Really basic tests about time stuff.
        /// </summary>
        [Fact]
        public void TestsAboutTime()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateRound(startTimestamp, minersCount, miningInterval);

            round.GetMiningInterval().ShouldBe(miningInterval);

            round.TotalMilliseconds().ShouldBe(miningInterval * minersCount + miningInterval);

            round.GetStartTime().ShouldBe(startTimestamp);

            round.GetExpectedEndTime().ShouldBe(startTimestamp.ToDateTime()
                .AddMilliseconds(minersCount * miningInterval + miningInterval).ToTimestamp());
        }

        [Fact]
        public void GetExpectedMiningTimeTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            round.GetExpectedMiningTime(firstMiner.PublicKey).ShouldBe(firstMiner.ExpectedMiningTime);
        }

        [Fact]
        public void IsTimeSlotPassed_Yes()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateRound(startTimestamp, minersCount, miningInterval);

            var firstMinerPublicKey = round.RealTimeMinersInformation.Keys.First();

            // Entered his time slot but not total passed.
            {
                var testTimestamp = startTimestamp.ToDateTime().AddMilliseconds(1).ToTimestamp();
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTimestamp, out _);

                isTimeSlotPassed.ShouldBe(true);
            }

            // Time slot passed but still in current round.
            {
                var testTimestamp = startTimestamp.ToDateTime().AddMilliseconds(2 * miningInterval).ToTimestamp();
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTimestamp, out _);

                isTimeSlotPassed.ShouldBe(true);
            }

            // Time slot passed and beyond current round.
            {
                var testTimestamp = startTimestamp.ToDateTime().AddMilliseconds(round.TotalMilliseconds())
                    .ToTimestamp();
                var isTimeSlotPassed = round.IsTimeSlotPassed(firstMinerPublicKey, testTimestamp, out _);

                isTimeSlotPassed.ShouldBe(true);
            }
        }

        [Fact]
        public void IsTimeSlotPassed_No()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();
            var testTimestamp = startTimestamp.ToDateTime().AddMilliseconds(-1).ToTimestamp();
            var isTimeSlotPassed = round.IsTimeSlotPassed(firstMiner.PublicKey, testTimestamp, out _);

            isTimeSlotPassed.ShouldBe(false);
        }

        [Fact]
        public void ArrangeAbnormalMiningTimeTest()
        {
            const int minersCount = 17;
            const int miningInterval = 4000;

            var startTimestamp = DateTime.UtcNow.ToTimestamp();

            var round = GenerateRound(startTimestamp, minersCount, miningInterval);

            var firstMiner = round.RealTimeMinersInformation.Values.First();

            // If the node hasn't miss his actual time slot.
            {
                var testTimestamp = startTimestamp.ToDateTime().AddMilliseconds(-1).ToTimestamp();
                var arrangedMiningTime = round.ArrangeAbnormalMiningTime(firstMiner.PublicKey, testTimestamp);

                arrangedMiningTime.ShouldBe(DateTime.MaxValue.ToUniversalTime().ToTimestamp());
            }

            // If this node just missed his time slot.
            {
                var testTimestamp = startTimestamp.ToDateTime().AddMilliseconds(1).ToTimestamp();
                var arrangedMiningTime = round.ArrangeAbnormalMiningTime(firstMiner.PublicKey, testTimestamp);

                arrangedMiningTime.ShouldBe(round.GetExpectedEndTime().ToDateTime()
                    .AddMilliseconds(miningInterval * firstMiner.Order).ToTimestamp());
            }

            // If this node noticed he missed his time slot several rounds later.
            {
                const int missedRoundsCount = 10;
                var testTimestamp = startTimestamp.ToDateTime()
                    .AddMilliseconds(1 + round.TotalMilliseconds() * missedRoundsCount).ToTimestamp();
                var arrangedMiningTime = round.ArrangeAbnormalMiningTime(firstMiner.PublicKey, testTimestamp);

                arrangedMiningTime.ShouldBe(round.GetExpectedEndTime(missedRoundsCount).ToDateTime()
                    .AddMilliseconds(miningInterval * firstMiner.Order).ToTimestamp());
            }
        }

        private Round GenerateRound(Timestamp startTimestamp, int minersCount, int miningInterval)
        {
            var round = new Round();
            var extraBlockProducerOrder = new Random().Next(1, minersCount);
            for (var i = 0; i < minersCount; i++)
            {
                var keyPair = CryptoHelpers.GenerateKeyPair();
                var minerInRound = new MinerInRound
                {
                    PublicKey = keyPair.PublicKey.ToHex(),
                    Order = i + 1,
                    Address = Address.FromPublicKey(keyPair.PublicKey),
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