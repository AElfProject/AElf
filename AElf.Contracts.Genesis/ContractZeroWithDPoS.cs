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
        private static readonly int MiningTime = Globals.MiningTime;

        // After the chain creator start a chain, wait for other mimers join
        private static readonly int WaitFirstRoundTime = Globals.WaitFirstRoundTime;

        // Block producers check interval
        private static readonly int CheckTime = Globals.CheckTime;

        private readonly UInt64Field _roundsCount = new UInt64Field("RoundsCount");
        
        private readonly PbField<BlockProducer> _blockProducer = new PbField<BlockProducer>("BPs");
        
        private readonly Map<UInt64Value, RoundInfo> _dPoSInfoMap = new Map<UInt64Value, RoundInfo>("DPoSInfo");
        
        // ReSharper disable once InconsistentNaming
        private readonly Map<UInt64Value, StringValue> _eBPMap = new Map<UInt64Value, StringValue>("EBP");
        
        private readonly PbField<Timestamp> _timeForProducingExtraBlock  = new PbField<Timestamp>("EBTime");

        private readonly PbField<Hash> _chainCreator = new PbField<Hash>("ChainCreator");

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap
            = new Map<UInt64Value, StringValue>("FirstPlaceOfEachRound");

        private readonly bool _printLog = true;
 
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
        
        #endregion
        
        #region Genesis block methods
        
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

        // ReSharper disable once InconsistentNaming
        public async Task SyncStateOfNextRound(RoundInfo suppliedPreviousRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            if (!await Authentication(nameof(SyncStateOfNextRound)))
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

            ConsoleWriteLine($"Sync dpos info of round {RoundsCount.Value} succeed");
        }

        #endregion

        #region BP Methods

        public async Task<BPInfo> PublishOutValueAndSignature(Hash outValue, Hash signature, UInt64Value roundsCount)
        {
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

        #endregion
        
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

            ConsoleWriteLine(accountAddress.Value + " may produced a block in an invalid timeslot:" + timeslotOfBlockProducer.ToDateTime().ToString("u"));

            return new BoolValue {Value = false};
        }
        
        public async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
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
            //ConsoleWriteLine($"Try to get bp {accountAddress}'s info of {roundsCount.Value} round");
            return (await _dPoSInfoMap.GetValueAsync(roundsCount)).Info[accountAddress];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(string accountAddress)
        {
            //ConsoleWriteLine($"Try to get bp {accountAddress}'s info of {RoundsCount.Value} round");
            return (await _dPoSInfoMap.GetValueAsync(RoundsCount)).Info[accountAddress];
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().ToHex().Remove(0, 2);
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

        private async Task<bool> Authentication(string methodName)
        {
            var fromAccount = ConvertToNormalHexString(Api.GetTransaction().From.Value.ToByteArray().ToHex());
            var result = (await GetBlockProducers()).Nodes.Contains(fromAccount);
            //ConsoleWriteLine($"Checked privilege to call consensus method {methodName}: {result}");
            return result;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string ConvertToNormalHexString(string hexStr)
        {
            return hexStr.StartsWith("0x") ? hexStr.Remove(0, 2) : hexStr;
        }

        private DateTime GetLocalTime()
        {
            return DateTime.UtcNow.ToLocalTime();
        }

        private void ConsoleWriteLine(string log)
        {
            if (_printLog)
            {
                Console.WriteLine($"[{GetLocalTime():HH:mm:ss} - DPoS]{log}");
            }
        }

        #endregion

        #endregion
    }
}