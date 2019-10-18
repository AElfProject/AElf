using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public bool GenerateNextRoundInformation(Timestamp currentBlockTimestamp, Timestamp blockchainStartTimestamp,
            out Round nextRound)
        {
            nextRound = new Round();

            var minersMinedCurrentRound = GetMinedMiners();
            var minersNotMinedCurrentRound = GetNotMinedMiners();
            var minersCount = RealTimeMinersInformation.Count;

            var miningInterval = GetMiningInterval();
            nextRound.RoundNumber = RoundNumber + 1;
            nextRound.TermNumber = TermNumber;
            nextRound.BlockchainAge = RoundNumber == 1 ? 1 : (currentBlockTimestamp - blockchainStartTimestamp).Seconds;

            // Set next round miners' information of miners who successfully mined during this round.
            foreach (var minerInRound in minersMinedCurrentRound.OrderBy(m => m.FinalOrderOfNextRound))
            {
                var order = minerInRound.FinalOrderOfNextRound;
                nextRound.RealTimeMinersInformation[minerInRound.Pubkey] = new MinerInRound
                {
                    Pubkey = minerInRound.Pubkey,
                    Order = order,
                    ExpectedMiningTime = currentBlockTimestamp.AddMilliseconds(miningInterval.Mul(order)),
                    ProducedBlocks = minerInRound.ProducedBlocks,
                    MissedTimeSlots = minerInRound.MissedTimeSlots
                };
            }

            // Set miners' information of miners missed their time slot in current round.
            var occupiedOrders = minersMinedCurrentRound.Select(m => m.FinalOrderOfNextRound).ToList();
            var ableOrders = Enumerable.Range(1, minersCount).Where(i => !occupiedOrders.Contains(i)).ToList();
            for (var i = 0; i < minersNotMinedCurrentRound.Count; i++)
            {
                var order = ableOrders[i];
                var minerInRound = minersNotMinedCurrentRound[i];
                nextRound.RealTimeMinersInformation[minerInRound.Pubkey] = new MinerInRound
                {
                    Pubkey = minersNotMinedCurrentRound[i].Pubkey,
                    Order = order,
                    ExpectedMiningTime = currentBlockTimestamp
                        .AddMilliseconds(miningInterval.Mul(order)),
                    ProducedBlocks = minerInRound.ProducedBlocks,
                    MissedTimeSlots = minerInRound.MissedTimeSlots + 1
                };
            }

            // Calculate extra block producer order and set the producer.
            var extraBlockProducerOrder = CalculateNextExtraBlockProducerOrder();
            var expectedExtraBlockProducer =
                nextRound.RealTimeMinersInformation.Values.FirstOrDefault(m => m.Order == extraBlockProducerOrder);
            if (expectedExtraBlockProducer == null)
            {
                nextRound.RealTimeMinersInformation.Values.First().IsExtraBlockProducer = true;
            }
            else
            {
                expectedExtraBlockProducer.IsExtraBlockProducer = true;
            }

            BreakContinuousMining(ref nextRound);

            nextRound.ConfirmedIrreversibleBlockHeight = ConfirmedIrreversibleBlockHeight;
            nextRound.ConfirmedIrreversibleBlockRoundNumber = ConfirmedIrreversibleBlockRoundNumber;

            return true;
        }

        private void BreakContinuousMining(ref Round nextRound)
        {
            var minersCount = RealTimeMinersInformation.Count;
            if (minersCount <= 1) return;

            // First miner of next round != Extra block producer of current round
            var firstMinerOfNextRound = nextRound.RealTimeMinersInformation.Values.First(i => i.Order == 1);
            var extraBlockProducerOfCurrentRound = GetExtraBlockProducerInformation();
            if (firstMinerOfNextRound.Pubkey == extraBlockProducerOfCurrentRound.Pubkey)
            {
                var secondMinerOfNextRound =
                    nextRound.RealTimeMinersInformation.Values.First(i => i.Order == 2);
                secondMinerOfNextRound.Order = 1;
                firstMinerOfNextRound.Order = 2;
                var tempTimestamp = secondMinerOfNextRound.ExpectedMiningTime;
                secondMinerOfNextRound.ExpectedMiningTime = firstMinerOfNextRound.ExpectedMiningTime;
                firstMinerOfNextRound.ExpectedMiningTime = tempTimestamp;
            }

            // Last miner of next round != Extra block producer of next round
            var lastMinerOfNextRound = nextRound.RealTimeMinersInformation.Values.First(i => i.Order == minersCount);
            var extraBlockProducerOfNextRound = nextRound.GetExtraBlockProducerInformation();
            if (lastMinerOfNextRound.Pubkey == extraBlockProducerOfNextRound.Pubkey)
            {
                var lastButOneMinerOfNextRound =
                    nextRound.RealTimeMinersInformation.Values.First(i => i.Order == minersCount.Sub(1));
                lastButOneMinerOfNextRound.Order = minersCount;
                lastMinerOfNextRound.Order = minersCount.Sub(1);
                var tempTimestamp = lastButOneMinerOfNextRound.ExpectedMiningTime;
                lastButOneMinerOfNextRound.ExpectedMiningTime = lastMinerOfNextRound.ExpectedMiningTime;
                lastMinerOfNextRound.ExpectedMiningTime = tempTimestamp;
            }
        }

        private int CalculateNextExtraBlockProducerOrder()
        {
            var firstPlaceInfo = RealTimeMinersInformation.Values.OrderBy(m => m.Order)
                .FirstOrDefault(m => m.Signature != null);
            if (firstPlaceInfo == null)
            {
                // If no miner produce block during this round, just appoint the first miner to be the extra block producer of next round.
                return 1;
            }

            var signature = firstPlaceInfo.Signature;
            var sigNum = signature.ToInt64();
            var blockProducerCount = RealTimeMinersInformation.Count;
            var order = GetAbsModulus(sigNum, blockProducerCount) + 1;
            return order;
        }

        public List<MinerInRound> GetMinedMiners()
        {
            // For now only this implementation can support test cases.
            return RealTimeMinersInformation.Values.Where(m => m.SupposedOrderOfNextRound != 0).ToList();
        }

        private List<MinerInRound> GetNotMinedMiners()
        {
            // For now only this implementation can support test cases.
            return RealTimeMinersInformation.Values.Where(m => m.SupposedOrderOfNextRound == 0).ToList();
        }
    }
}