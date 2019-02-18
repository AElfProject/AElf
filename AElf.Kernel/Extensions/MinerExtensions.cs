using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    // ReSharper disable InconsistentNaming
    public static class MinersExtensions
    {
        public static bool IsEmpty(this Miners miners)
        {
            return !miners.PublicKeys.Any();
        }

        public static Hash GetMinersHash(this Miners miners)
        {
            return Hash.FromMessage(miners.PublicKeys.OrderBy(p => p).ToMiners());
        }

        public static Term GenerateNewTerm(this Miners miners, int miningInterval, ulong roundNumber = 0, ulong termNumber = 0)
        {
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var miner in miners.PublicKeys)
            {
                dict.Add(miner, miner[0]);
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();

            var infosOfRound1 = new Round();

            var selected = new Random().Next(0, miners.PublicKeys.Count);
            for (var i = 0; i < enumerable.Count; i++)
            {
                var minerInRound = new MinerInRound {IsExtraBlockProducer = false};

                if (i == selected)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.Order = i + 1;
                minerInRound.Signature = Hash.Generate();
                minerInRound.ExpectedMiningTime =
                    GetTimestampOfUtcNow(i * miningInterval + GlobalConfig.AElfWaitFirstRoundTime);
                minerInRound.PublicKey = enumerable[i];

                infosOfRound1.RealTimeMinersInfo.Add(enumerable[i], minerInRound);
            }

            // Second round
            dict = new Dictionary<string, int>();

            foreach (var miner in miners.PublicKeys)
            {
                dict.Add(miner, miner[0]);
            }

            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            enumerable = sortedMiningNodes.ToList();

            var infosOfRound2 = new Round();

            var totalSecondsOfFirstRound = (enumerable.Count + 1) * miningInterval;

            selected = new Random().Next(0, miners.PublicKeys.Count);
            for (var i = 0; i < enumerable.Count; i++)
            {
                var minerInRound = new MinerInRound {IsExtraBlockProducer = false};

                if (i == selected)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }

                minerInRound.ExpectedMiningTime =
                    GetTimestampOfUtcNow(i * miningInterval + totalSecondsOfFirstRound +
                                         GlobalConfig.AElfWaitFirstRoundTime);
                minerInRound.Order = i + 1;
                minerInRound.PublicKey = enumerable[i];

                infosOfRound2.RealTimeMinersInfo.Add(enumerable[i], minerInRound);
            }

            infosOfRound1.RoundNumber = roundNumber + 1;
            infosOfRound2.RoundNumber = roundNumber + 2;

            infosOfRound1.MiningInterval = miningInterval;
            infosOfRound2.MiningInterval = miningInterval;

            var term = new Term
            {
                TermNumber = termNumber + 1,
                FirstRound = infosOfRound1,
                SecondRound = infosOfRound2,
                Miners = new Miners
                {
                    TermNumber = termNumber + 1,
                    PublicKeys = {miners.PublicKeys}
                },
                MiningInterval = miningInterval,
                Timestamp = DateTime.UtcNow.ToTimestamp()
            };

            return term;
        }

        public static Round GenerateNextRound(this Miners miners, int chainId, Round previousRound)
        {
            if (previousRound.RoundNumber == 1)
            {
                return new Round {RoundNumber = 0};
            }

            var miningInterval = previousRound.MiningInterval;
            var round = new Round {RoundNumber = previousRound.RoundNumber + 1};

            // EBP will be the first miner of next round.
            var extraBlockProducer = previousRound.GetExtraBlockProducerInformation().PublicKey;

            var signatureDict = new Dictionary<Hash, string>();
            var orderDict = new Dictionary<int, string>();

            var blockProducerCount = previousRound.RealTimeMinersInfo.Count;

            if (chainId.DumpBase58() == GlobalConfig.DefaultChainId ||
                previousRound.RealTimeMinersInfo.Keys.Union(miners.PublicKeys).Count() == miners.PublicKeys.Count)
            {
                foreach (var miner in previousRound.RealTimeMinersInfo.Values)
                {
                    var s = miner.Signature;
                    if (s == null)
                    {
                        s = Hash.Generate();
                    }

                    signatureDict[s] = miner.PublicKey;
                }

                foreach (var sig in signatureDict.Keys)
                {
                    var sigNum = BitConverter.ToUInt64(
                        BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
                    var order = Math.Abs(GetModulus(sigNum, blockProducerCount));

                    if (orderDict.ContainsKey(order))
                    {
                        for (var i = 0; i < blockProducerCount; i++)
                        {
                            if (!orderDict.ContainsKey(i))
                            {
                                order = i;
                            }
                        }
                    }

                    orderDict.Add(order, signatureDict[sig]);
                }
            }
            else
            {
                extraBlockProducer = miners.PublicKeys[0];
                for (var i = 0; i < blockProducerCount; i++)
                {
                    orderDict.Add(i, miners.PublicKeys[i]);
                }
            }

            var extraBlockMiningTime = previousRound.GetEBPMiningTime(miningInterval).ToTimestamp();

            // Maybe because something happened with setting extra block time slot.
            if (extraBlockMiningTime.ToDateTime().AddMilliseconds(miningInterval * 1.5) <
                GetTimestampOfUtcNow().ToDateTime())
            {
                extraBlockMiningTime = GetTimestampOfUtcNow();
            }

            for (var i = 0; i < orderDict.Count; i++)
            {
                var minerPublicKey = orderDict[i];
                var minerInRound = new MinerInRound
                {
                    ExpectedMiningTime =
                        GetTimestampWithOffset(extraBlockMiningTime, i * miningInterval + miningInterval),
                    Order = i + 1,
                    PublicKey = minerPublicKey
                };

                round.RealTimeMinersInfo[minerPublicKey] = minerInRound;
            }

            var newEBPOrder = CalculateNextExtraBlockProducerOrder(previousRound);
            var newEBP = round.RealTimeMinersInfo.Keys.ToList()[newEBPOrder];
            round.RealTimeMinersInfo[newEBP].IsExtraBlockProducer = true;

            if (GlobalConfig.BlockProducerNumber != 1)
            {
                // Exchange
                var orderOfEBP = round.RealTimeMinersInfo[extraBlockProducer].Order;
                var expectedMiningTimeOfEBP = round.RealTimeMinersInfo[extraBlockProducer].ExpectedMiningTime;

                round.RealTimeMinersInfo[extraBlockProducer].Order = 1;
                round.RealTimeMinersInfo[extraBlockProducer].ExpectedMiningTime =
                    round.RealTimeMinersInfo.First().Value.ExpectedMiningTime;

                round.RealTimeMinersInfo.First().Value.Order = orderOfEBP;
                round.RealTimeMinersInfo.First().Value.ExpectedMiningTime = expectedMiningTimeOfEBP;
            }

            round.MiningInterval = previousRound.MiningInterval;

            return round;
        }

        private static int CalculateNextExtraBlockProducerOrder(Round roundInfo)
        {
            var firstPlaceInfo = roundInfo.GetFirstPlaceMinerInfo();
            var sig = firstPlaceInfo.Signature;
            if (sig == null)
            {
                sig = Hash.Generate();
            }

            var sigNum = BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
            var blockProducerCount = roundInfo.RealTimeMinersInfo.Count;
            var order = GetModulus(sigNum, blockProducerCount);

            return order;
        }
        
        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        private static Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            var now = Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
            return now;
        }

        private static Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }

        /// <summary>
        /// In case of forgetting to check negative value.
        /// For now this method only used for generating order,
        /// so integer should be enough.
        /// </summary>
        /// <param name="uLongVal"></param>
        /// <param name="intVal"></param>
        /// <returns></returns>
        private static int GetModulus(ulong uLongVal, int intVal)
        {
            return Math.Abs((int) (uLongVal % (ulong) intVal));
        }
    }
}