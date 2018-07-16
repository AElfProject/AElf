using System;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    public class DPoSCheck
    {
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly ECKeyPair _keyPair;
        private readonly IDataProvider _dataProvider;
        private readonly Hash _chainId;
        private readonly BlockProducer _blockProducer;
        private readonly Hash _contractAddressHash;
        private readonly ILogger _logger;
        
        private UInt64Value RoundsCount
        {
            get
            {
                var bytes = _dataProvider.GetAsync("RoundsCount".CalculateHash()).Result;
                return UInt64Value.Parser.ParseFrom(bytes);
            }
        }

        public DPoSCheck(IWorldStateDictator worldStateDictator, ECKeyPair keyPair, Hash chainId, BlockProducer blockProducer, Hash contractAddressHash, ILogger logger)
        {
            _worldStateDictator = worldStateDictator.SetChainId(chainId);
            _keyPair = keyPair;
            _chainId = chainId;
            _blockProducer = blockProducer;
            _contractAddressHash = contractAddressHash;
            _logger = logger;

            _dataProvider = _worldStateDictator.GetAccountDataProvider(contractAddressHash).Result.GetDataProvider();

        }

        public async Task<bool> AbleToMine()
        {
            var accountHash = _keyPair.GetAddress();
            var accountAddress = AddressHashToString(accountHash);
            var now = GetTimestampOfUtcNow();

            if (!_blockProducer.Nodes.Contains(accountAddress))
            {
                return false;
            }
            
            var assignedTimeSlot = await GetTimeSlot(accountAddress);
            var timeSlotEnd = GetTimestamp(assignedTimeSlot, Globals.MiningTime);
            
            return CompareTimestamp(now, assignedTimeSlot) && CompareTimestamp(timeSlotEnd, now);
        }

        public async Task<bool> ReadyForHelpingProducingExtraBlock()
        {
            var me = _keyPair.GetAddress();
            var meAddress = AddressHashToString(me);
            
            // ReSharper disable once InconsistentNaming
            var currentEBP = StringValue.Parser
                .ParseFrom(await _dataProvider.GetDataProvider("EBP").GetAsync(RoundsCount.CalculateHash()));

            var meOrder = (await GetBlockProducerInfoOfCurrentRound(meAddress)).Order;
            // ReSharper disable once InconsistentNaming
            var currentEBPOrder = (await GetBlockProducerInfoOfCurrentRound(currentEBP.Value)).Order;
            var blockProducerCount = _blockProducer.Nodes.Count;
            var orderDiff = meOrder - currentEBPOrder;
            if (orderDiff < 0)
            {
                orderDiff = blockProducerCount + orderDiff;
            }

            var timeOfARound = Globals.MiningTime * blockProducerCount + Globals.CheckTime + Globals.MiningTime;

            var assignedExtraBlockProducingTime = Timestamp.Parser.ParseFrom(await _dataProvider.GetAsync("EBTime".CalculateHash()));
            var assignedExtraBlockProducingTimeOfNextRound = GetTimestamp(assignedExtraBlockProducingTime, timeOfARound);
            var assigendExtraBlockProducingTimeOfNextRoundEnd =
                GetTimestamp(assignedExtraBlockProducingTimeOfNextRound, Globals.CheckTime + Globals.MiningTime);
            
            var now = GetTimestampOfUtcNow();

            var offset = Globals.MiningTime * orderDiff - Globals.MiningTime;
            
            var assigendExtraBlockProducingTimeEndWithOffset = GetTimestamp(assigendExtraBlockProducingTimeOfNextRoundEnd, offset);

            var timeDiff = (now - assigendExtraBlockProducingTimeOfNextRoundEnd).Seconds * 1000;
            
            var currentTimeslot = timeDiff % timeOfARound;

            var afterTime = (offset - timeDiff) / 1000;

            if (meAddress == StringValue.Parser
                    .ParseFrom(await _dataProvider.GetDataProvider("EBP").GetAsync(RoundsCount.CalculateHash())).Value)
            {
                _logger?.Trace($"I am the EBP of this round - RoundCount:{RoundsCount}");
                afterTime = (assignedExtraBlockProducingTimeOfNextRound - now).Seconds;
            }

            if (afterTime < 0)
            {
                //The only reason to come here is checking ability after expected timeslot
                //So the abs of afterTime should not greater than CheckTime
                if (afterTime < -Globals.MiningTime)
                {
                    _logger?.Trace($"Something weird happened to ready-for-help checking");
                }
                afterTime = 0;
            }
            
            if (timeDiff > timeOfARound)
            {
                afterTime = afterTime + timeOfARound;
            }

            _logger?.Trace(CompareTimestamp(assignedExtraBlockProducingTime, now)
                ? $"Will publish In Value after {(assignedExtraBlockProducingTime - now).Seconds}s"
                : $"Will (help to) produce extra block after {afterTime}s");

            if (afterTime > 0)
            {
                return false;
            }
            
            if (currentTimeslot > offset && currentTimeslot < offset + Globals.MiningTime)
            {
                return true;
            }
            
            if (orderDiff == blockProducerCount - 1)
            {
                return CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset);
            }

            return CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset)
                   && CompareTimestamp(GetTimestamp(assigendExtraBlockProducingTimeEndWithOffset, Globals.MiningTime), now);
        }

        private async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }

        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(string accountAddress)
        {
            var bytes = await _dataProvider.GetDataProvider("DPoSInfo").GetAsync(RoundsCount.CalculateHash());
            var roundInfo = RoundInfo.Parser.ParseFrom(bytes);
            return roundInfo.Info[accountAddress];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfSpecificRound(string accountAddress, UInt64Value roundsCount)
        {
            var bytes = await _dataProvider.GetDataProvider("DPoSInfo").GetAsync(roundsCount.CalculateHash());
            var roundInfo = RoundInfo.Parser.ParseFrom(bytes);
            return roundInfo.Info[accountAddress];
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().ToHex().Remove(0, 2);
        }
        
        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
        }

        private Timestamp GetTimestamp(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }
        
        /// <summary>
        /// Return true if ts1 >= ts2
        /// </summary>
        /// <param name="ts1"></param>
        /// <param name="ts2"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() >= ts2.ToDateTime();
        }
    }
}