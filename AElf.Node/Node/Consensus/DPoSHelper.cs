using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    public class DPoSHelper
    {
        private readonly ECKeyPair _keyPair;
        private readonly IDataProvider _dataProvider;
        private readonly BlockProducer _blockProducer;
        private readonly ILogger _logger;
        
        public UInt64Value RoundsCount
        {
            get
            {
                var bytes = _dataProvider.GetAsync("RoundsCount".CalculateHash()).Result;
                return bytes == null ? new UInt64Value {Value = 0} : UInt64Value.Parser.ParseFrom(bytes);
            }
        }

        public DPoSHelper(IWorldStateDictator worldStateDictator, ECKeyPair keyPair, Hash chainId, BlockProducer blockProducer, Hash contractAddressHash, ILogger logger)
        {
            worldStateDictator.SetChainId(chainId);
            _keyPair = keyPair;
            _blockProducer = blockProducer;
            _logger = logger;
            _logger = null;

            _dataProvider = worldStateDictator.GetAccountDataProvider(contractAddressHash).Result.GetDataProvider();

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

            // ReSharper disable once InconsistentNaming
            var isEBP = meAddress == StringValue.Parser
                            .ParseFrom(await _dataProvider.GetDataProvider("EBP").GetAsync(RoundsCount.CalculateHash()))
                            .Value;
            if (isEBP)
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

            if (afterTime % 17000 == 0 && isEBP)
            {
                return true;
            }
            
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
        
        public DPoSInfo GenerateInfoForFirstTwoRounds()
        {
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var node in _blockProducer.Nodes)
            {
                dict.Add(node, node[0]);
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound1 = new RoundInfo();

            var selected = _blockProducer.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};
                
                if (i == selected)
                {
                    bpInfo.IsEBP = true;

                }

                bpInfo.Order = i + 1;
                bpInfo.Signature = Hash.Generate();
                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * Globals.MiningTime + Globals.WaitFirstRoundTime);

                infosOfRound1.Info.Add(enumerable[i], bpInfo);
            }

            // Second round
            dict = new Dictionary<string, int>();
            
            foreach (var node in _blockProducer.Nodes)
            {
                dict.Add(node, node[0]);
            }
            
            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;
            
            enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound2 = new RoundInfo();

            var addition = enumerable.Count * Globals.MiningTime + Globals.MiningTime;

            selected = _blockProducer.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                }

                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * Globals.MiningTime + addition + Globals.WaitFirstRoundTime);
                bpInfo.Order = i + 1;

                infosOfRound2.Info.Add(enumerable[i], bpInfo);
            }

            var dPoSInfo = new DPoSInfo
            {
                RoundInfo = {infosOfRound1, infosOfRound2}
            };
            
            return dPoSInfo;
        }
        
        public async Task<bool> IsTimeToProduceExtraBlock()
        {
            if (!_blockProducer.Nodes.Contains(AddressHashToString(_keyPair.GetAddress())))
            {
                return false;
            }

            var expectedTime = Timestamp.Parser.ParseFrom(await _dataProvider.GetAsync("EBTime".CalculateHash()));
            var now = GetTimestampOfUtcNow();
            return CompareTimestamp(now, expectedTime)
                   && CompareTimestamp(GetTimestamp(expectedTime, Globals.MiningTime), now);
        }
        
        public async Task<bool> AbleToProduceExtraBlock()
        {
            var accountHash = _keyPair.GetAddress();

            // ReSharper disable once InconsistentNaming
            var eBP = StringValue.Parser
                .ParseFrom(await _dataProvider.GetDataProvider("EBP").GetAsync(RoundsCount.CalculateHash())).Value;
            
            return AddressHashToString(accountHash) == eBP;
        }


        public async Task<RoundInfo> SupplyPreviousRoundInfo()
        {
            try
            {
                var roundInfo =
                    RoundInfo.Parser.ParseFrom(await _dataProvider.GetDataProvider("DPoSInfo")
                        .GetAsync(RoundsCount.CalculateHash()));

                foreach (var info in roundInfo.Info)
                {
                    if (info.Value.InValue != null && info.Value.OutValue != null) continue;

                    var inValue = Hash.Generate();
                    var outValue = inValue.CalculateHash();

                    info.Value.OutValue = outValue;
                    info.Value.InValue = inValue;

                    //For the first round, the sig value is auto generated
                    if (info.Value.Signature == null && RoundsCount.Value != 1)
                    {
                        var signature = await CalculateSignature(inValue);
                        info.Value.Signature = signature;
                    }

                    roundInfo.Info[info.Key] = info.Value;
                }

                return roundInfo;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to supply previous round info");
                return new RoundInfo();
            }
        }
        
        public async Task<Hash> CalculateSignature(Hash inValue)
        {
            try
            {
                var add = Hash.Default;
                foreach (var node in _blockProducer.Nodes)
                {
                    var bpInfo = await GetBlockProducerInfoOfSpecificRound(node, RoundsCountMinusOne(RoundsCount));
                    var lastSignature = bpInfo.Signature;
                    add = add.CalculateHashWith(lastSignature);
                }

                Hash sig = inValue.CalculateHashWith(add);
                return sig;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to calculate signature");
                return Hash.Default;
            }
        }

        public async Task<RoundInfo> GenerateNextRoundOrder()
        {
            try
            {
                var infosOfNextRound = new RoundInfo();
                var signatureDict = new Dictionary<Hash, string>();
                var orderDict = new Dictionary<int, string>();

                var blockProducerCount = _blockProducer.Nodes.Count;

                foreach (var node in _blockProducer.Nodes)
                {
                    var s = (await GetBlockProducerInfoOfCurrentRound(node)).Signature;
                    if (s == null)
                    {
                        s = Hash.Generate();
                    }

                    signatureDict[s] = node;
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

                for (var i = 0; i < orderDict.Count; i++)
                {
                    var bpInfoNew = new BPInfo
                    {
                        TimeSlot = GetTimestampOfUtcNow(i * Globals.MiningTime + Globals.MiningTime),
                        Order = i + 1
                    };

                    infosOfNextRound.Info[orderDict[i]] = bpInfoNew;
                }

                return infosOfNextRound;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to generate info of next round");
                return new RoundInfo();
            }
        }

        public async Task<StringValue> SetNextExtraBlockProducer()
        {
            try
            {
                var firstPlace = StringValue.Parser.ParseFrom(await _dataProvider.GetDataProvider("FirstPlaceOfEachRound")
                    .GetAsync(RoundsCount.CalculateHash()));
                var firstPlaceInfo = await GetBlockProducerInfoOfCurrentRound(firstPlace.Value);
                var sig = firstPlaceInfo.Signature;
                if (sig == null)
                {
                    sig = Hash.Generate();
                }
            
                var sigNum = BitConverter.ToUInt64(
                    BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
                var blockProducerCount = _blockProducer.Nodes.Count;
                var order = GetModulus(sigNum, blockProducerCount);

                // ReSharper disable once InconsistentNaming
                var nextEBP = _blockProducer.Nodes[order];
            
                return new StringValue {Value = nextEBP};
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to set next extra block producer");
                return new StringValue {Value = ""};
            }
        }
        
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public async Task<StringValue> GetDPoSInfoToString()
        {
            ulong count = 1;

            if (RoundsCount.Value != 0)
            {
                count = RoundsCount.Value;
            }
            var infoOfOneRound = "";

            ulong i = 1;
            while (i <= count)
            {
                var roundInfoStr = await GetRoundInfoToString(new UInt64Value {Value = i});
                infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                i++;
            }

            // ReSharper disable once InconsistentNaming
            var eBPTimeslot = Timestamp.Parser.ParseFrom(await _dataProvider.GetAsync("EBTime".CalculateHash()));

            var res = new StringValue
            {
                Value
                    = infoOfOneRound + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
                             + "Current Round : " + RoundsCount?.Value
            };
            
            return res;
        }

        // ReSharper disable once InconsistentNaming
        public async Task<string> GetDPoSInfoToStringOfLatestRounds(ulong countOfRounds)
        {
            try
            {
                if (RoundsCount.Value == 0)
                {
                    return "No DPoS Information, maybe failed to sync blocks";
                }
            
                var currentRoundsCount = RoundsCount.Value;
                ulong startRound;
                if (countOfRounds >= currentRoundsCount)
                {
                    startRound = 1;
                }
                else
                {
                    startRound = currentRoundsCount - countOfRounds + 1;
                }

                var infoOfOneRound = "";
                var i = startRound;
                while (i <= currentRoundsCount)
                {
                    if (i <= 0)
                    {
                        continue;
                    }

                    var roundInfoStr = await GetRoundInfoToString(new UInt64Value {Value = i});
                    infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                    i++;
                }
            
                // ReSharper disable once InconsistentNaming
                var eBPTimeslot = Timestamp.Parser.ParseFrom(await _dataProvider.GetAsync("EBTime".CalculateHash()));

                return infoOfOneRound + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
                                      + $"Current Round : {RoundsCount.Value}";
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to get dpos info");
                return "";
            }
        }

        private async Task<string> GetRoundInfoToString(UInt64Value roundsCount)
        {
            try
            {
                var info = RoundInfo.Parser.ParseFrom(await _dataProvider.GetDataProvider("DPoSInfo")
                    .GetAsync(roundsCount.CalculateHash()));
                var result = "";

                foreach (var bpInfo in info.Info)
                {
                    result += bpInfo.Key + ":\n";
                    result += "IsEBP:\t\t" + bpInfo.Value.IsEBP + "\n";
                    result += "Order:\t\t" + bpInfo.Value.Order + "\n";
                    result += "Timeslot:\t" + bpInfo.Value.TimeSlot.ToDateTime().ToLocalTime().ToString("u") + "\n";
                    result += "Signature:\t" + bpInfo.Value.Signature?.Value.ToByteArray().ToHex().Remove(0, 2) + "\n";
                    result += "Out Value:\t" + bpInfo.Value.OutValue?.Value.ToByteArray().ToHex().Remove(0, 2) + "\n";
                    result += "In Value:\t" + bpInfo.Value.InValue?.Value.ToByteArray().ToHex().Remove(0, 2) + "\n";
                }

                return result + "\n";
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Failed to get dpos info of round {roundsCount.Value}");
                return "";
            }
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountMinusOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current--;
            return new UInt64Value {Value = current};
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
        
        /// <summary>
        /// In case of forgetting to check negtive value.
        /// For now this method only used for generating order,
        /// so integer should be enough.
        /// </summary>
        /// <param name="uLongVal"></param>
        /// <param name="intVal"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private int GetModulus(ulong uLongVal, int intVal)
        {
            return Math.Abs((int) (uLongVal % (ulong) intVal));
        }
    }
}