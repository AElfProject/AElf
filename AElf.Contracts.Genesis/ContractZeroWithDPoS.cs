using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using SharpRepository.Repository.Configuration;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Genesis
{
    // ReSharper disable once InconsistentNaming
    public class ContractZeroWithDPoS : BasicContractZero
    {
        #region DPoS

        // The length of one timeslot for a miner to produce block
        private const int MiningTime = 8000;

        // After the chain creator start a chain, wait for other mimers join
        private const int WaitFirstRoundTime = 16000;

        // Block producers check interval
        private const int CheckTime = 3000;

        private readonly UInt64Field _roundsCount = new UInt64Field("RoundsCount");
        
        private readonly PbField<BlockProducer> _blockProducer = new PbField<BlockProducer>("BPs");
        
        private readonly Map<UInt64Value, RoundInfo> _dPoSInfoMap = new Map<UInt64Value, RoundInfo>("DPoSInfo");
        
        // ReSharper disable once InconsistentNaming
        private readonly Map<UInt64Value, StringValue> _eBPMap = new Map<UInt64Value, StringValue>("EBP");
        
        private readonly PbField<Timestamp> _timeForProducingExtraBlock  = new PbField<Timestamp>("EBTime");

        private readonly PbField<Hash> _chainCreator = new PbField<Hash>("ChainCreator");

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap
            = new Map<UInt64Value, StringValue>("FirstPlaceOfEachRound");
        
        private readonly object _lock;
 
        private UInt64Value RoundsCount => new UInt64Value {Value = _roundsCount.GetAsync().Result};
        
        #region Block Producers
        
        public async Task<BlockProducer> GetBlockProducers()
        {
            // Should be setted before
            var blockProducer = await _blockProducer.GetAsync();

            if (blockProducer.Nodes.Count < 1)
            {
                throw new ConfigurationErrorsException("No block producer.");
            }

            return blockProducer;
        }

        public async Task<BlockProducer> SetBlockProducers(BlockProducer blockProducers)
        {
            if (await _chainCreator.GetAsync() != null)
            {
                return null;
            }

            await _blockProducer.SetAsync(blockProducers);

            return blockProducers;
        }
        
        #endregion
        
        #region Genesis block methods
        
        public async Task<DPoSInfo> GenerateInfoForFirstTwoRounds(BlockProducer blockProducers)
        {
            var dict = new Dictionary<string, int>();

            await _blockProducer.SetAsync(blockProducers);

            // First round
            foreach (var node in blockProducers.Nodes)
            {
                dict.Add(node, node[2]);
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound1 = new RoundInfo();

            var selected = blockProducers.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};
                
                if (i == selected)
                {
                    bpInfo.IsEBP = true;

                }

                bpInfo.Order = i + 1;
                bpInfo.Signature = Hash.Generate();
                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * MiningTime + WaitFirstRoundTime);

                infosOfRound1.Info.Add(enumerable[i], bpInfo);
            }

            // Second round
            dict = new Dictionary<string, int>();
            
            foreach (var node in blockProducers.Nodes)
            {
                dict.Add(node, node[2]);
            }
            
            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;
            
            enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound2 = new RoundInfo();

            var addition = enumerable.Count * MiningTime + MiningTime;

            selected = blockProducers.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                }

                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * MiningTime + addition + WaitFirstRoundTime);
                bpInfo.Order = i + 1;

                infosOfRound2.Info.Add(enumerable[i], bpInfo);
            }

            var dPoSInfo = new DPoSInfo
            {
                RoundInfo = {infosOfRound1, infosOfRound2}
            };
            
            return dPoSInfo;
        }

        public async Task SyncStateOfFirstTwoRounds(DPoSInfo dPoSInfo, BlockProducer blockProducer)
        {
            await _blockProducer.SetAsync(blockProducer);
            
            var firstRound = new UInt64Value {Value = 1};
            var secondRound = new UInt64Value {Value = 2};

            await _roundsCount.SetAsync(1);

            await _firstPlaceMap.SetValueAsync(firstRound,
                new StringValue {Value = dPoSInfo.RoundInfo[0].Info.First().Key});
            await _firstPlaceMap.SetValueAsync(secondRound,
                new StringValue {Value = dPoSInfo.RoundInfo[1].Info.First().Key});

            await _dPoSInfoMap.SetValueAsync(firstRound, dPoSInfo.RoundInfo[0]);
            await _dPoSInfoMap.SetValueAsync(secondRound, dPoSInfo.RoundInfo[1]);

            // ReSharper disable once InconsistentNaming
            var eBPOfRound1 = dPoSInfo.RoundInfo[0].Info.First(bp => bp.Value.IsEBP);
            // ReSharper disable once InconsistentNaming
            var eBPOfRound2 = dPoSInfo.RoundInfo[1].Info.First(bp => bp.Value.IsEBP);
            await _eBPMap.SetValueAsync(firstRound, new StringValue {Value = eBPOfRound1.Key});
            await _eBPMap.SetValueAsync(secondRound, new StringValue {Value = eBPOfRound2.Key});

            await _timeForProducingExtraBlock.SetAsync(
                GetTimestamp(dPoSInfo.RoundInfo[0].Info.Last().Value.TimeSlot, MiningTime));

            await _chainCreator.SetAsync(Api.GetTransaction().From);
        }
        
        #endregion

        #region EBP Methods

        public async Task<RoundInfo> GenerateNextRoundOrder()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var infosOfNextRound = new RoundInfo();
            var signatureDict = new Dictionary<Hash, string>();
            var orderDict = new Dictionary<int, string>();

            var blockProducer = await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;

            foreach (var node in blockProducer.Nodes)
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
                    TimeSlot = GetTimestampOfUtcNow(i * MiningTime + MiningTime),
                    Order = i + 1
                };

                infosOfNextRound.Info[orderDict[i]] = bpInfoNew;
            }

            return infosOfNextRound;
        }
        
        public async Task<StringValue> SetNextExtraBlockProducer()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var firstPlace = await _firstPlaceMap.GetValueAsync(RoundsCount);
            var firstPlaceInfo = await GetBlockProducerInfoOfCurrentRound(firstPlace.Value);
            var sig = firstPlaceInfo.Signature;
            if (sig == null)
            {
                sig = Hash.Generate();
            }
            
            var sigNum = BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
            var blockProducer = await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;
            var order = GetModulus(sigNum, blockProducerCount);

            // ReSharper disable once InconsistentNaming
            var nextEBP = blockProducer.Nodes[order];
            
            return new StringValue {Value = nextEBP};
        }
        
        public async Task<UInt64Value> SetRoundsCount()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var newRoundsCount = RoundsCountAddOne(RoundsCount);

            return newRoundsCount;
        }
        
        public async Task<UInt64Value> GetRoundsCount()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            return new UInt64Value {Value = await _roundsCount.GetAsync()};
        }

        // ReSharper disable once InconsistentNaming
        public async Task SyncStateOfNextRound(RoundInfo suppliedPreviousRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            if (!await Authentication())
            {
                return;
            }
            
            await _eBPMap.SetValueAsync(RoundsCountAddOne(RoundsCount), nextEBP);
            nextRoundInfo.Info.First(info => info.Key == nextEBP.Value).Value.IsEBP = true;

            var currentRoundInfo = await _dPoSInfoMap.GetValueAsync(RoundsCount);

            foreach (var infoPair in currentRoundInfo.Info)
            {
                if (infoPair.Value.InValue != null) 
                    continue;
                
                var supplyValue = suppliedPreviousRoundInfo.Info.First(info => info.Key == infoPair.Key)
                    .Value;
                infoPair.Value.InValue = supplyValue.InValue;
                infoPair.Value.OutValue = supplyValue.OutValue;
                infoPair.Value.Signature = supplyValue.Signature;
            }
            await _dPoSInfoMap.SetValueAsync(RoundsCount, currentRoundInfo);
            
            await _dPoSInfoMap.SetValueAsync(RoundsCountAddOne(RoundsCount), nextRoundInfo);

            await _firstPlaceMap.SetValueAsync(RoundsCountAddOne(RoundsCount), new StringValue {Value = nextRoundInfo.Info.First().Key});

            await _timeForProducingExtraBlock.SetAsync(GetTimestamp(nextRoundInfo.Info.Last().Value.TimeSlot,
                MiningTime + CheckTime));

            //Update the rounds count at last
            await _roundsCount.SetAsync(RoundsCountAddOne(RoundsCount).Value);

            Console.WriteLine($"Sync dpos info of round {RoundsCount.Value} succeed");
        }

        #endregion

        public async Task<BoolValue> ReadyForHelpingProducingExtraBlock()
        {
            if (!await Authentication())
            {
                return null;
            }

            var me = Api.GetTransaction().From;
            var meAddress = AddressHashToString(me);
            
            // ReSharper disable once InconsistentNaming
            var currentEBP = await _eBPMap.GetValueAsync(RoundsCount);

            var meOrder = (await GetBlockProducerInfoOfCurrentRound(meAddress)).Order;
            // ReSharper disable once InconsistentNaming
            var currentEBPOrder = (await GetBlockProducerInfoOfCurrentRound(currentEBP.Value)).Order;
            var blockProducerCount = (await GetBlockProducers()).Nodes.Count;
            var orderDiff = meOrder - currentEBPOrder;
            if (orderDiff < 0)
            {
                orderDiff = blockProducerCount + orderDiff;
            }

            var assignedExtraBlockProducingTime = await _timeForProducingExtraBlock.GetAsync();
            var assigendExtraBlockProducingTimeEnd =
                GetTimestamp(assignedExtraBlockProducingTime, CheckTime + MiningTime);

            var now = GetTimestampOfUtcNow();

            var offset = MiningTime * orderDiff - MiningTime;
            var assigendExtraBlockProducingTimeEndWithOffset = GetTimestamp(assigendExtraBlockProducingTimeEnd, offset);

            var timeOfARound = MiningTime * blockProducerCount + CheckTime + MiningTime;
            var timeDiff = (now - assigendExtraBlockProducingTimeEnd).Seconds * 1000;
            var currentTimeslot = timeDiff % timeOfARound;

            var afterTime = (offset - currentTimeslot) / 1000;
            
            if (meAddress == (await _eBPMap.GetValueAsync(RoundsCount)).Value)
            {
                Console.WriteLine($"[{GetTimestampOfUtcNow().ToDateTime().ToLocalTime():T} - DPoS]: I am the EBP of this round - RoundCount:" + RoundsCount);
                afterTime = (assignedExtraBlockProducingTime - now).Seconds;
            }
            
            if (afterTime < 0)
            {
                afterTime = 0;
            }
            
            Console.WriteLine($"[{GetTimestampOfUtcNow().ToDateTime().ToLocalTime():T} - DPoS]: Will produce extra block after {afterTime}s");

            if (afterTime > 0)
            {
                return new BoolValue {Value = false};
            }
            
            if (currentTimeslot > offset && currentTimeslot < offset + MiningTime)
            {
                Console.WriteLine("currentTimeslot:" + currentTimeslot);
                Console.WriteLine("offset:" + offset);
                return new BoolValue {Value = true};
            }
            
            if (orderDiff == blockProducerCount - 1)
            {
                return new BoolValue
                {
                    Value = CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset)
                };
            }
            
            return new BoolValue
            {
                Value = CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset)
                         && CompareTimestamp(GetTimestamp(assigendExtraBlockProducingTimeEndWithOffset, MiningTime), now)
                        
            };
        }

        #region BP Methods

        public async Task<BPInfo> PublishOutValueAndSignature(Hash outValue, Hash signature, UInt64Value roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            
            var info = await GetBlockProducerInfoOfSpecificRound(accountAddress, roundsCount);
            
            info.OutValue = outValue;
            if (roundsCount.Value > 1)
                info.Signature = signature;
            
            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundsCount);
            roundInfo.Info[accountAddress] = info;
            
            await _dPoSInfoMap.SetValueAsync(roundsCount, roundInfo);

            return info;
        }

        public async Task<Hash> TryToPublishInValue(Hash inValue, UInt64Value roundsCount)
        {
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            
            var info = await GetBlockProducerInfoOfSpecificRound(accountAddress, roundsCount);
            info.InValue = inValue;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundsCount);
            roundInfo.Info[accountAddress] = info;

            await _dPoSInfoMap.SetValueAsync(roundsCount, roundInfo);

            return inValue;
        }

        /// <summary>
        /// Supplement of Round info.
        /// </summary>
        /// <returns></returns>
        public async Task<RoundInfo> SupplyPreviousRoundInfo()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var roundInfo = await _dPoSInfoMap.GetValueAsync(RoundsCount);

            foreach (var info in roundInfo.Info)
            {
                if (info.Value.InValue == null || info.Value.OutValue == null)
                {
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
            }

            //await _dPoSInfoMap.SetValueAsync(RoundsCount, roundInfo);

            return roundInfo;
        }
        
        #endregion
        
        public async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }

        public async Task<Hash> GetInValueOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount}))?.InValue;
        }
        
        public async Task<Hash> GetOutValueOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.OutValue;
        }
        
        public async Task<Hash> GetSignatureOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.Signature;
        }
        
        public async Task<int?> GetOrderOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.Order;
        }
        
        public async Task<Hash> CalculateSignature(Hash inValue)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var add = Hash.Default;
            var blockProducer = await GetBlockProducers();
            foreach (var node in blockProducer.Nodes)
            {
                var bpInfo = await GetBlockProducerInfoOfSpecificRound(node, RoundsCountMinusOne(RoundsCount));
                var lastSignature = bpInfo.Signature;
                add = add.CalculateHashWith(lastSignature);
            }

            Hash sig = inValue.CalculateHashWith(add);
            return sig;
        }
        
        public async Task<bool> AbleToMine()
        {
            if (!await Authentication())
            {
                return false;
            }
            
            var accountHash = Api.GetTransaction().From;
            var accountAddress = AddressHashToString(accountHash);
            var now = GetTimestampOfUtcNow();

            if (!await IsBP(accountAddress))
            {
                return false;
            }
            
            var assignedTimeSlot = await GetTimeSlot(accountAddress);
            var timeSlotEnd = GetTimestamp(assignedTimeSlot, MiningTime);
            
            return CompareTimestamp(now, assignedTimeSlot) && CompareTimestamp(timeSlotEnd, now);
        }

        // ReSharper disable once InconsistentNaming
        public async Task<StringValue> GetEBPOf(UInt64Value roundsCount)
        {
            return await _eBPMap.GetValueAsync(roundsCount);
        }
        
        // ReSharper disable once InconsistentNaming
        public async Task<StringValue> GetCurrentEBP()
        {
            return await _eBPMap.GetValueAsync(RoundsCount);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<bool> IsBP(string accountAddress)
        {
            var blockProducer = await GetBlockProducers();
            return blockProducer.Nodes.Contains(accountAddress);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<bool> IsEBP(string accountAddress)
        {
            var info = await GetBlockProducerInfoOfCurrentRound(accountAddress);
            return info.IsEBP;
        }
        
        public async Task<bool> IsTimeToProduceExtraBlock()
        {
            var expectedTime = await _timeForProducingExtraBlock.GetAsync();
            var now = GetTimestampOfUtcNow();
            return CompareTimestamp(now, expectedTime)
                   && CompareTimestamp(GetTimestamp(expectedTime, MiningTime), now);
        }
        
        public async Task<bool> AbleToProduceExtraBlock()
        {
            var accountHash = Api.GetTransaction().From;
            
            // ReSharper disable once InconsistentNaming
            var eBP = await _eBPMap.GetValueAsync(RoundsCount);
            
            return AddressHashToString(accountHash) == eBP.Value;
        }

        // ReSharper disable once InconsistentNaming
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
            var eBPTimeslot = await _timeForProducingExtraBlock.GetAsync();

            var res = new StringValue
            {
                Value
                    = infoOfOneRound + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
                             + "Current Round : " + RoundsCount?.Value
            };
            
            return res;
        }

        // ReSharper disable once InconsistentNaming
        public async Task<StringValue> GetDPoSInfoToStringOfLatestRounds(UInt64Value countOfRounds)
        {
            if (RoundsCount.Value == 0)
            {
                return new StringValue {Value = "No DPoS Information, maybe failed to sync blocks"};
            }
            
            var currentRoundsCount = RoundsCount.Value;
            ulong startRound;
            if (countOfRounds.Value >= currentRoundsCount)
            {
                startRound = 1;
            }
            else
            {
                startRound = currentRoundsCount - countOfRounds.Value + 1;
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
            var eBPTimeslot = await _timeForProducingExtraBlock.GetAsync();

            return new StringValue
            {
                Value
                    = infoOfOneRound + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
                                     + "Current Round : " + RoundsCount.Value
            };
        }

        public async Task<string> GetRoundInfoToString(UInt64Value roundsCount)
        {
            var info = await _dPoSInfoMap.GetValueAsync(roundsCount);
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

        public async Task<BoolValue> BlockProducerVerification(StringValue accountAddress)
        {
            if (!await IsBP(accountAddress.Value))
            {
                return new BoolValue {Value = false};
            }

            var now = GetTimestampOfUtcNow();
            var timeslotOfBlockProducer = await GetTimeSlot(accountAddress.Value);
            var endOfTimeslotOfBlockProducer = GetTimestamp(timeslotOfBlockProducer, MiningTime);
            // ReSharper disable once InconsistentNaming
            var timeslotOfEBP = await _timeForProducingExtraBlock.GetAsync();
            if (CompareTimestamp(now, timeslotOfBlockProducer) && CompareTimestamp(endOfTimeslotOfBlockProducer, now) ||
                CompareTimestamp(now, timeslotOfEBP))
            {
                return new BoolValue {Value = true};
            }

            var start = RoundsCount.Value;
            for (var i = start; i > 0; i--)
            {
                var blockProducerInfo =
                    await GetBlockProducerInfoOfSpecificRound(accountAddress.Value, new UInt64Value {Value = i});
                var timeslot = blockProducerInfo.TimeSlot;
                var timeslotEnd = GetTimestamp(timeslot, MiningTime);
                if (CompareTimestamp(now, timeslot) && CompareTimestamp(timeslotEnd, now))
                {
                    return new BoolValue {Value = true};
                }
            }

            Console.WriteLine(accountAddress.Value + "may produce a block with invalid timeslot:" + timeslotOfBlockProducer.ToDateTime().ToString("u"));

            return new BoolValue {Value = false};
        }

        #region Private Methods

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
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountAddOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current++;
            return new UInt64Value {Value = current};
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private UInt64Value RoundsCountMinusOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current--;
            return new UInt64Value {Value = current};
        }

        private async Task<BPInfo> GetBlockProducerInfoOfSpecificRound(string accountAddress, UInt64Value roundsCount)
        {
            return (await _dPoSInfoMap.GetValueAsync(roundsCount)).Info[accountAddress];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(string accountAddress)
        {
            return (await _dPoSInfoMap.GetValueAsync(RoundsCount)).Info[accountAddress];
        }

        
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().Value.ToByteArray().ToHex();
        }

        private Hash HexStringToHash(string accountAddress)
        {
            return ByteArrayHelpers.FromHexString(accountAddress);
        }

        /// <summary>
        /// In case of forgetting to check negtive value
        /// </summary>
        /// <param name="uLongVal"></param>
        /// <param name="intVal"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private int GetModulus(ulong uLongVal, int intVal)
        {
            var m = (int) uLongVal % intVal;

            return Math.Abs(m);
        }

        private async Task<bool> Authentication()
        {
            var fromAccount = Api.GetTransaction().From.Value.ToByteArray().ToHex();
            return (await GetBlockProducers()).Nodes.Contains(fromAccount);
        }

        #endregion

        #endregion
    }
}