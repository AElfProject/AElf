using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AElfConsensus
{
    internal partial class Round
    {
        public bool GenerateNextRoundInformation(DateTime dateTime, Timestamp blockchainStartTimestamp, out Round nextRound)
        {
            nextRound = new Round();

            var minersMinedCurrentRound = GetMinedMiners();
            var minersNotMinedCurrentRound = GetNotMinedMiners();
            var minersCount = RealTimeMinersInformation.Count;

            var miningInterval = GetMiningInterval();
            nextRound.RoundNumber = RoundNumber + 1;
            nextRound.TermNumber = TermNumber;
            if (RoundNumber == 1)
            {
                nextRound.BlockchainAge = 1;
            }
            else
            {
                nextRound.BlockchainAge =
                    (long) (dateTime - blockchainStartTimestamp.ToDateTime())
                    .TotalMinutes; // TODO: Change to TotalDays after testing.
            }

            // Set next round miners' information of miners who successfully mined during this round.
            foreach (var minerInRound in minersMinedCurrentRound.OrderBy(m => m.FinalOrderOfNextRound))
            {
                var order = minerInRound.FinalOrderOfNextRound;
                nextRound.RealTimeMinersInformation[minerInRound.PublicKey] = new MinerInRound
                {
                    PublicKey = minerInRound.PublicKey,
                    Order = order,
                    ExpectedMiningTime = dateTime.AddMilliseconds(miningInterval * order).ToTimestamp(),
                    PromisedTinyBlocks = minerInRound.PromisedTinyBlocks,
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
                nextRound.RealTimeMinersInformation[minerInRound.PublicKey] = new MinerInRound
                {
                    PublicKey = minersNotMinedCurrentRound[i].PublicKey,
                    Order = order,
                    ExpectedMiningTime = dateTime.AddMilliseconds(miningInterval * order).ToTimestamp(),
                    PromisedTinyBlocks = minerInRound.PromisedTinyBlocks,
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

            return true;
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
            var sigNum = BitConverter.ToInt64(
                BitConverter.IsLittleEndian ? signature.Value.Reverse().ToArray() : signature.Value.ToArray(), 0);
            var blockProducerCount = RealTimeMinersInformation.Count;
            var order = GetAbsModulus(sigNum, blockProducerCount) + 1;
            return order;
        }
        
        public List<MinerInRound> GetMinedMiners()
        {
            // For now only this implementation can support test cases.
            return RealTimeMinersInformation.Values.Where(m => m.SupposedOrderOfNextRound != 0).ToList();
        }
        
        public List<MinerInRound> GetNotMinedMiners()
        {
            // For now only this implementation can support test cases.
            return RealTimeMinersInformation.Values.Where(m => m.SupposedOrderOfNextRound == 0).ToList();
        }
    }
}