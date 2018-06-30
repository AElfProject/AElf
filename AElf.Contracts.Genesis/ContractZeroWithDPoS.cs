using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp.MetadataAttribute;
using SharpRepository.Repository.Configuration;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using NServiceKit.Logging;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Genesis
{
    public class ContractZeroWithDPoS : BasicContractZero
    {
        #region DPoS

        // The length of one timeslot for a miner to produce block
        private const int MiningTime = 16000;

        // After the chain creator start a chain, wait for other mimers join
        private const int WaitFirstRoundTime = 16000;

        // Block producers check interval
        private const int CheckTime = 5000;

        [SmartContractFieldData("${this}._roundsCount", DataAccessMode.ReadWriteAccountSharing)]
        private readonly UInt64Field _roundsCount = new UInt64Field("RoundsCount");
        
        [SmartContractFieldData("${this}._blockProducer", DataAccessMode.ReadWriteAccountSharing)]
        private readonly PbField<BlockProducer> _blockProducer = new PbField<BlockProducer>("BPs");
        
        [SmartContractFieldData("${this}._dPoSInfoMap", DataAccessMode.ReadWriteAccountSharing)]
        private readonly Map<UInt64Value, RoundInfo> _dPoSInfoMap = new Map<UInt64Value, RoundInfo>("DPoSInfo");
        
        // ReSharper disable once InconsistentNaming
        [SmartContractFieldData("${this}._eBPMap", DataAccessMode.ReadWriteAccountSharing)]
        private readonly Map<UInt64Value, StringValue> _eBPMap = new Map<UInt64Value, StringValue>("EBP");
        
        [SmartContractFieldData("${this}._timeForProducingExtraBlock", DataAccessMode.ReadWriteAccountSharing)]
        private readonly PbField<Timestamp> _timeForProducingExtraBlock  = new PbField<Timestamp>("EBTime");

        [SmartContractFieldData("${this}._chainCreator", DataAccessMode.ReadWriteAccountSharing)]
        private readonly PbField<Hash> _chainCreator = new PbField<Hash>("ChainCreator");

        [SmartContractFieldData("${this}._firstPlaceMap", DataAccessMode.ReadWriteAccountSharing)]
        private readonly Map<UInt64Value, StringValue> _firstPlaceMap
            = new Map<UInt64Value, StringValue>("FirstPlaceOfEachRound");
        
        [SmartContractFieldData("${this}._lock", DataAccessMode.ReadWriteAccountSharing)]
        private readonly object _lock;
 
        private UInt64Value RoundsCount => new UInt64Value {Value = _roundsCount.GetAsync().Result};
        
        #region Block Producers
        
        [SmartContractFunction("${this}.GetBlockProducers", new string[]{}, new []{"${this}._blockProducer"})]
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

        [SmartContractFunction("${this}.SetBlockProducers", new string[]{}, new []{"${this}._blockProducer", "${this}._chainCreator"})]
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
        
        [SmartContractFunction("${this}.GenerateInfoForFirstTwoRounds", new string[]{"${this}.GetTimestampOfUtcNow"}, new string[]{})]
        public async Task<DPoSInfo> GenerateInfoForFirstTwoRounds(BlockProducer blockProducers)
        {
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var node in blockProducers.Nodes)
            {
                dict.Add(node, node[0]);
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
                dict.Add(node, node[0]);
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

        [SmartContractFunction("${this}.SyncStateOfFirstTwoRounds", new string[]{"${this}.Authentication", "${this}.GetTimestamp", "${this}.CompareTimestamp"}, new string[]{"${this}._blockProducer", "${this}._roundsCount", "${this}._firstPlaceMap", "${this}._dPoSInfoMap", "${this}._eBPMap", "${this}._timeForProducingExtraBlock", "${this}._chainCreator"})]
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

        [SmartContractFunction("${this}.GenerateNextRoundOrder", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfCurrentRound", "${this}.RoundsCountAddOne","${this}.GetTimestamp",  "${this}.CompareTimestamp", "${this}.GetBlockProducers"}, new string[]{"${this}._dPoSInfoMap", "${this}._roundsCount"})]
        public async Task<RoundInfo> GenerateNextRoundOrder()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            if (RoundsCount.Value == 1)
            {
                return await _dPoSInfoMap.GetValueAsync(RoundsCountAddOne(RoundsCount));
            }

            var infosOfNextRound = new RoundInfo();
            var signatureDict = new Dictionary<Hash, string>();
            var orderDict = new Dictionary<int, string>();

            var blockProducer = await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;

            foreach (var node in blockProducer.Nodes) 
                signatureDict[(await GetBlockProducerInfoOfCurrentRound(node)).Signature] = node;

            foreach (var sig in signatureDict.Keys)
            {
                var sigNum = BitConverter.ToUInt64(
                    BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
                var order = GetModulus(sigNum, blockProducerCount);

                if (order < 0)
                {
                    order = -order;
                }
 
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
                var bpInfoNew = new BPInfo();

                var timeForExtraBlockOfLastRound = await _timeForProducingExtraBlock.GetAsync();
                bpInfoNew.TimeSlot = GetTimestamp(timeForExtraBlockOfLastRound, i * MiningTime + MiningTime);
                bpInfoNew.Order = i + 1;

                infosOfNextRound.Info[orderDict[i]] = bpInfoNew;
            }

            return infosOfNextRound;
        }
        
        [SmartContractFunction("${this}.SetNextExtraBlockProducer", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfCurrentRound", "${this}.RoundsCountAddOne", "${this}.GetBlockProducers"}, new string[]{"${this}._firstPlaceMap", "${this}._roundsCount"})]
        public async Task<StringValue> SetNextExtraBlockProducer()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var firstPlace = await _firstPlaceMap.GetValueAsync(RoundsCount);
            var firstPlaceInfo = await GetBlockProducerInfoOfCurrentRound(firstPlace.Value);
            var sig = firstPlaceInfo.Signature;
            var sigNum = BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
            var blockProducer = await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;
            var order = GetModulus(sigNum, blockProducerCount);
            
            if (RoundsCount.Value == 1)
            {
                var round = await _dPoSInfoMap.GetValueAsync(RoundsCountAddOne(RoundsCount));
                // ReSharper disable once InconsistentNaming
                var eBPOfRound2 = round.Info.FirstOrDefault(i => i.Value.IsEBP).Key;
                //Set extra block timeslot for next round
                return new StringValue { Value = eBPOfRound2};
            }
            
            // ReSharper disable once InconsistentNaming
            var nextEBP = blockProducer.Nodes[order];
            
            return new StringValue {Value = nextEBP};
        }
        
        [SmartContractFunction("${this}.SetRoundsCount", new string[]{"${this}.RoundsCountAddOne"}, new string[]{"${this}._roundsCount"})]
        public async Task<UInt64Value> SetRoundsCount()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var newRoundsCount = RoundsCountAddOne(RoundsCount);
            await _roundsCount.SetAsync(newRoundsCount.Value);

            return newRoundsCount;
        }
        
        [SmartContractFunction("${this}.GetRoundsCount", new string[]{"${this}.Authentication"}, new string[]{"${this}._roundsCount"})]
        public async Task<UInt64Value> GetRoundsCount()
        {
            if (!await Authentication())
            {
                return null;
            }
            
            return new UInt64Value {Value = await _roundsCount.GetAsync()};
        }

        // ReSharper disable once InconsistentNaming
        [SmartContractFunction("${this}.SyncStateOfNextRound", new string[]{"${this}.Authentication", "${this}.GetTimestamp", "${this}.RoundsCountAddOne", "${this}.CompareTimestamp"}, new string[]{"${this}._roundsCount", "${this}._eBPMap", "${this}._dPoSInfoMap", "${this}._firstPlaceMap",   "${this}._timeForProducingExtraBlock" })]
        public async Task SyncStateOfNextRound(RoundInfo suppliedPreviousRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            if (!await Authentication())
            {
                return;
            }
            
            if (RoundsCount.Value != 1)
            {
                await _eBPMap.SetValueAsync(RoundsCountAddOne(RoundsCount), nextEBP);
                nextRoundInfo.Info.First(info => info.Key == nextEBP.Value).Value.IsEBP = true;
            }

            var currentRoundInfo = await _dPoSInfoMap.GetValueAsync(RoundsCount);

            foreach (var infoPair in currentRoundInfo.Info)
            {
                if (infoPair.Value.InValue != null || RoundsCount.Value == 1) 
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

            Console.WriteLine($"Sync dpos info of round {RoundsCountAddOne(RoundsCount).Value} succeed");
        }

        #endregion

        [SmartContractFunction("${this}.ReadyForHelpingProducingExtraBlock", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfCurrentRound","${this}.GetTimestamp",  "${this}.CompareTimestamp", "${this}.GetTimestampOfUtcNow", "${this}.GetBlockProducers"}, new string[]{"${this}._roundsCount", "${this}._timeForProducingExtraBlock" })]
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

            if (meAddress == currentEBP.Value)
            {
                return new BoolValue {Value = false};
            }
            
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

            if (orderDiff == blockProducerCount - 1)
            {
                return new BoolValue
                {
                    Value = CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset)
                };
            }
            
            return new BoolValue
            {
                Value = (CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset)
                         && CompareTimestamp(GetTimestamp(assigendExtraBlockProducingTimeEndWithOffset, MiningTime), now)) ||
                        //todo: if more than two nodes wake up suddenly after next round's timeslot, this will cause problem
                        CompareTimestamp(now, GetTimestamp(assigendExtraBlockProducingTimeEnd, MiningTime * blockProducerCount))
            };
        }

        #region BP Methods

        [SmartContractFunction("${this}.PublishOutValueAndSignature", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound"}, new string[]{"${this}._dPoSInfoMap" })]
        public async Task<BPInfo> PublishOutValueAndSignature(Hash outValue, Hash signature, UInt64Value roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            
            Console.WriteLine("For round:" + roundsCount.Value + " of " + accountAddress);

            var info = await GetBlockProducerInfoOfSpecificRound(accountAddress, roundsCount);
            
            info.OutValue = outValue;
            if (roundsCount.Value > 1)
                info.Signature = signature;
            
            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundsCount);
            roundInfo.Info[accountAddress] = info;
            
            await _dPoSInfoMap.SetValueAsync(roundsCount, roundInfo);

            return info;
        }

        [SmartContractFunction("${this}.TryToPublishInValue", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound"}, new string[]{"${this}._dPoSInfoMap" })]
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
        [SmartContractFunction("${this}.SupplyPreviousRoundInfo", new string[]{"${this}.Authentication", "${this}.CalculateSignature"}, new string[]{"${this}._dPoSInfoMap", "${this}._roundsCount" })]
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
                    if (info.Value.Signature == null || info.Value.Signature.Value.Length == 0)
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
        
        [SmartContractFunction("${this}.GetTimeSlot", new string[]{"${this}.GetBlockProducerInfoOfCurrentRound"}, new string[]{ })]
        public async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }

        [SmartContractFunction("${this}.GetInValueOf", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound"}, new string[]{ "${this}._roundsCount" })]
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
        
        [SmartContractFunction("${this}.GetOutValueOf", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound"}, new string[]{ "${this}._roundsCount" })]
        public async Task<Hash> GetOutValueOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.OutValue;
        }
        
        [SmartContractFunction("${this}.GetSignatureOf", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound"}, new string[]{ "${this}._roundsCount" })]
        public async Task<Hash> GetSignatureOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.Signature;
        }
        
        [SmartContractFunction("${this}.GetOrderOf", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound"}, new string[]{ "${this}._roundsCount" })]
        public async Task<int?> GetOrderOf(string accountAddress, ulong roundsCount)
        {
            if (!await Authentication())
            {
                return null;
            }
            
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.Order;
        }
        
        [SmartContractFunction("${this}.CalculateSignature", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfSpecificRound", "${this}.RoundsCountMinusOne", "${this}.GetBlockProducers"}, new string[]{ "${this}._roundsCount" })]
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
        
        [SmartContractFunction("${this}.AbleToMine", new string[]{"${this}.Authentication", "${this}.GetTimestamp", "${this}.CompareTimestamp", "${this}.GetTimestampOfUtcNow", "${this}.IsBP", "${this}.GetTimeSlot"}, new string[]{})]
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
        [SmartContractFunction("${this}.GetEBPOf", new string[]{"${this}.Authentication"}, new string[]{"${this}._eBPMap"})]
        public async Task<StringValue> GetEBPOf(UInt64Value roundsCount)
        {
            return await _eBPMap.GetValueAsync(roundsCount);
        }
        
        // ReSharper disable once InconsistentNaming
        [SmartContractFunction("${this}.GetCurrentEBP", new string[]{"${this}.Authentication"}, new string[]{"${this}._eBPMap", "${this}._roundsCount"})]
        public async Task<StringValue> GetCurrentEBP()
        {
            return await _eBPMap.GetValueAsync(RoundsCount);
        }
        
        // ReSharper disable once InconsistentNaming
        [SmartContractFunction("${this}.IsBP", new string[]{"${this}.GetBlockProducers"}, new string[]{})]
        private async Task<bool> IsBP(string accountAddress)
        {
            var blockProducer = await GetBlockProducers();
            return blockProducer.Nodes.Contains(accountAddress);
        }
        
        // ReSharper disable once InconsistentNaming
        [SmartContractFunction("${this}.IsEBP", new string[]{"${this}.Authentication", "${this}.GetBlockProducerInfoOfCurrentRound"}, new string[]{})]
        private async Task<bool> IsEBP(string accountAddress)
        {
            var info = await GetBlockProducerInfoOfCurrentRound(accountAddress);
            return info.IsEBP;
        }
        
        [SmartContractFunction("${this}.IsTimeToProduceExtraBlock", new string[]{"${this}.Authentication", "${this}.GetTimestamp", "${this}.CompareTimestamp", "${this}.GetTimestampOfUtcNow"}, new string[]{"${this}._timeForProducingExtraBlock"})]
        public async Task<bool> IsTimeToProduceExtraBlock()
        {
            var expectedTime = await _timeForProducingExtraBlock.GetAsync();
            var now = GetTimestampOfUtcNow();
            return CompareTimestamp(now, expectedTime)
                   && CompareTimestamp(GetTimestamp(expectedTime, MiningTime), now);
        }
        
        [SmartContractFunction("${this}.AbleToProduceExtraBlock", new string[]{"${this}.Authentication"}, new string[]{"${this}._eBPMap", "${this}._roundsCount"})]
        public async Task<bool> AbleToProduceExtraBlock()
        {
            var accountHash = Api.GetTransaction().From;
            
            // ReSharper disable once InconsistentNaming
            var eBP = await _eBPMap.GetValueAsync(RoundsCount);
            
            return AddressHashToString(accountHash) == eBP.Value;
        }

        // ReSharper disable once InconsistentNaming
        [SmartContractFunction("${this}.GetDPoSInfoToString", new string[]{"${this}.Authentication", "${this}.GetRoundInfoToString"}, new string[]{"${this}._timeForProducingExtraBlock", "${this}._roundsCount"})]
        public async Task<StringValue> GetDPoSInfoToString()
        {
            ulong count = 1;

            if (RoundsCount != null)
            {
                count = RoundsCount.Value;
            }
            var result = "";

            ulong i = 1;
            while (i <= count)
            {
                var roundInfoStr = await GetRoundInfoToString(new UInt64Value {Value = i});
                result += $"\n[Round {i}]\n" + roundInfoStr;
                i++;
            }

            // ReSharper disable once InconsistentNaming
            var eBPTimeslot = await _timeForProducingExtraBlock.GetAsync();

            var res = new StringValue
            {
                Value
                    = result + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
                             + "Current Round : " + RoundsCount?.Value
            };
            
            return res;
        }

        [SmartContractFunction("${this}.GetRoundInfoToString", new string[]{"${this}.Authentication"}, new string[]{"${this}._dPoSInfoMap"})]
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
                result += "Signature:\t" + bpInfo.Value.Signature + "\n";
                result += "Out Value:\t" + bpInfo.Value.OutValue + "\n";
                result += "In Value:\t" + bpInfo.Value.InValue + "\n";
            }

            return result + "\n";
        }

        [SmartContractFunction("${this}.BlockProducerVerification", new string[]{"${this}.IsBP", "${this}.GetTimestampOfUtcNow", "${this}.GetTimeSlot", "${this}.CompareTimestamp"}, new string[]{"${this}._timeForProducingExtraBlock"})]
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
            return new BoolValue
            {
                Value = (CompareTimestamp(now, timeslotOfBlockProducer) && CompareTimestamp(endOfTimeslotOfBlockProducer, now))
                        || CompareTimestamp(now, timeslotOfEBP)
            };
        }

        #region Private Methods

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        [SmartContractFunction("${this}.GetTimestampOfUtcNow", new string[]{}, new string[]{})]
        private Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
        }

        [SmartContractFunction("${this}.GetTimestamp", new string[]{}, new string[]{})]
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
        [SmartContractFunction("${this}.CompareTimestamp", new string[]{}, new string[]{})]
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() >= ts2.ToDateTime();
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        [SmartContractFunction("${this}.RoundsCountAddOne", new string[]{}, new string[]{})]
        private UInt64Value RoundsCountAddOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current++;
            return new UInt64Value {Value = current};
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        [SmartContractFunction("${this}.RoundsCountMinusOne", new string[]{}, new string[]{})]
        private UInt64Value RoundsCountMinusOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current--;
            return new UInt64Value {Value = current};
        }

        [SmartContractFunction("${this}.GetBlockProducerInfoOfSpecificRound", new string[]{}, new string[]{"${this}._dPoSInfoMap"})]
        private async Task<BPInfo> GetBlockProducerInfoOfSpecificRound(string accountAddress, UInt64Value roundsCount)
        {
            return (await _dPoSInfoMap.GetValueAsync(roundsCount)).Info[accountAddress];
        }
        
        [SmartContractFunction("${this}.GetBlockProducerInfoOfCurrentRound", new string[]{}, new string[]{"${this}._dPoSInfoMap", "${this}._roundsCount"})]
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

        [SmartContractFunction("${this}.Authentication", new string[]{"${this}.GetBlockProducers"}, new string[]{})]
        private async Task<bool> Authentication()
        {
            var fromAccount = Api.GetTransaction().From.Value.ToByteArray().ToHex();
            return (await GetBlockProducers()).Nodes.Contains(fromAccount);
        }

        #endregion

        #endregion
        
    }
}