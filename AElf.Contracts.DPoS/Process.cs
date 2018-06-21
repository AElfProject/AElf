using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Akka.Actor;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using SharpRepository.Repository.Configuration;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.DPoS
{
    public class Process : CSharpSmartContract
    {
        private const int MiningTime = 4;

        private readonly UInt64Field _roundsCount = new UInt64Field("RoundsCount");
        
        private readonly PbField<BlockProducer> _blockProducer = new PbField<BlockProducer>("BPs");
        
        private readonly Map<UInt64Value, RoundInfo> _dPoSInfoMap = new Map<UInt64Value, RoundInfo>("DPoSInfo");
        
        // ReSharper disable once InconsistentNaming
        private readonly Map<UInt64Value, StringValue> _eBPMap = new Map<UInt64Value, StringValue>("EBP");
        
        private readonly PbField<Timestamp> _timeForProducingExtraBlock  = new PbField<Timestamp>("EBTime");
        
        private readonly Map<UInt64Value, StringValue> _firstPlaceMap
            = new Map<UInt64Value, StringValue>("FirstPlaceOfEachRound");
 
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
            
            Api.Return(blockProducer);

            return blockProducer;
        }

        #region Hide for now

        /*public async Task<object> SetBlockProducers()
        {
            List<string> miningNodes;
            
            //TODO: Temp impl.
            using (var file = 
                File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.DPoS/MiningNodes.txt")))
            {
                miningNodes = file.ReadLines().ToList();
            }

            var blockProducers = new BlockProducer();
            foreach (var node in miningNodes)
            {
                blockProducers.Nodes.Add(node);
            }

            if (blockProducers.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find block producers in related config file.");
            }

            await _blockProducer.SetAsync(blockProducers);

            return blockProducers;
        }*/

        #endregion

        public async Task<BlockProducer> SetBlockProducers(BlockProducer blockProducers)
        {
            if (blockProducers.Nodes.Count < 1)
            {
                throw new InvalidOperationException("Cannot find mining nodes in related config file.");
            }

            await _blockProducer.SetAsync(blockProducers);
            
            Api.Return(blockProducers);

            return blockProducers;
        }
        
        #endregion
        
        #region Genesis block methods
        
        public async Task<DPoSInfo> RandomizeInfoForFirstTwoRounds()
        {
            var blockProducers = await GetBlockProducers();
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var node in blockProducers.Nodes)
            {
                var random = new Random(DateTime.Now.Millisecond + node[2]);
                dict.Add(node, random.Next(0, 1000));
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound1 = new RoundInfo();

            await _roundsCount.SetAsync(1);

            var selected = new Random(DateTime.Now.Millisecond).Next(7, enumerable.Count - 1);
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                    await _eBPMap.SetValueAsync(RoundsCount, new StringValue {Value = enumerable[i]});

                }

                bpInfo.Order = i + 1;
                bpInfo.Signature = Hash.Generate();
                bpInfo.TimeSlot = GetTimestamp(i * MiningTime);

                if (i == enumerable.Count - 1)
                {
                    await _timeForProducingExtraBlock.SetAsync(GetTimestamp(i * MiningTime + MiningTime));
                }

                infosOfRound1.Info.Add(enumerable[i], bpInfo);
            }
            
            await _dPoSInfoMap.SetValueAsync(RoundsCount, infosOfRound1);

            // Second round
            dict = new Dictionary<string, int>();
            
            foreach (var node in blockProducers.Nodes)
            {
                var random = new Random(DateTime.Now.Millisecond + node[7]);
                dict.Add(node, random.Next(0, 1000));
            }
            
            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;
            
            enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound2 = new RoundInfo();
            
            await _roundsCount.SetAsync(2);
            selected = new Random(DateTime.Now.Millisecond).Next(2, enumerable.Count - 1);
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                    await _eBPMap.SetValueAsync(RoundsCount, new StringValue {Value = enumerable[i]});
                }

                bpInfo.TimeSlot = GetTimestamp(i * MiningTime);
                bpInfo.Order = i + 1;

                if (i == enumerable.Count - 1)
                {
                    await _timeForProducingExtraBlock.SetAsync(GetTimestamp(i * MiningTime + MiningTime));
                }

                infosOfRound2.Info.Add(enumerable[i], bpInfo);
            }
            
            await _dPoSInfoMap.SetValueAsync(RoundsCount, infosOfRound2);

            return new DPoSInfo
            {
                RoundInfo = {infosOfRound1, infosOfRound2}
            };
        }
        
        #endregion

        public async Task<string> GenerateNextRoundOrder()
        {
            var infosOfNextRound = new RoundInfo();

            var bpInfo = await GetBlockProducerInfoOfCurrentRound(Api.GetTransaction().From);
            
            
            if (!bpInfo.IsEBP)
            {
                return "1";
            }
            
            var blockProducer = await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;

            var signatureDict = new Dictionary<Hash, string>();
            foreach (var node in blockProducer.Nodes)
            {
                signatureDict[(await GetBlockProducerInfoOfCurrentRound(node)).Signature] = node;
            }

            var orderDict = new Dictionary<int, string>();
            foreach (var sig in signatureDict.Keys)
            {
                var sigNum = BitConverter.ToUInt64(
                    BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
                var order = (int) sigNum % blockProducerCount;
                orderDict.Add(
                    orderDict.ContainsKey(order)
                        ? Enumerable.Range(0, blockProducerCount - 1).First(n => !orderDict.ContainsKey(n))
                        : order,
                    signatureDict[sig]);
            }
                        
            for (var i = 0; i < orderDict.Count; i++)
            {
                var bpInfoNew = new BPInfo();

                if (i == 0)
                {
                    await _firstPlaceMap.SetValueAsync(RoundsCountAddOne(RoundsCount), new StringValue {Value = orderDict[0]});
                }

                bpInfoNew.TimeSlot = GetTimestamp(i * MiningTime);
                bpInfoNew.Order = i + 1;
                
                if (i == orderDict.Count - 1)
                {
                    await _timeForProducingExtraBlock.SetAsync(GetTimestamp(i * MiningTime + MiningTime));
                }

                infosOfNextRound.Info[orderDict[i]] = bpInfoNew;
            }

            await _dPoSInfoMap.SetValueAsync(RoundsCountAddOne(RoundsCount), infosOfNextRound);

            return "end";
        }

        public async Task<object> SetNextExtraBlockProducer()
        {
            var firstPlace = await _firstPlaceMap.GetValueAsync(RoundsCount);
            var firstPlaceInfo = await GetBlockProducerInfoOfCurrentRound(firstPlace.Value);
            var sig = firstPlaceInfo.Signature;
            var sigNum = BitConverter.ToUInt64(
                BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
            var blockProducer = await GetBlockProducers();
            var blockProducerCount = blockProducer.Nodes.Count;
            var order = (int) sigNum % blockProducerCount;
            // ReSharper disable once InconsistentNaming
            var nextEBP = blockProducer.Nodes[order];
            await _eBPMap.SetValueAsync(RoundsCountAddOne(RoundsCount), new StringValue {Value = nextEBP});

            return null;
        }

        public async Task<object> SetRoundsCount()
        {
            await _roundsCount.SetAsync(RoundsCountAddOne(RoundsCount).Value);

            return null;
        }

        public async Task<BPInfo> PublishOutValueAndSignature(Hash outValue, Hash signature)
        {
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            var info = await GetBlockProducerInfoOfCurrentRound(accountAddress);
            info.OutValue = outValue;
            info.Signature = signature;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(RoundsCount);
            roundInfo.Info[accountAddress] = info;

            await _dPoSInfoMap.SetValueAsync(RoundsCount, roundInfo);

            return info;
        }

        public async Task<object> TryToPublishInValue(Hash inValue)
        {
            if (!(bool)await IsTimeToProduceExtraBlock())
            {
                return null;
            }
            
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            var info = await GetBlockProducerInfoOfCurrentRound(accountAddress);
            info.InValue = inValue;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(RoundsCount);
            roundInfo.Info[accountAddress] = info;

            await _dPoSInfoMap.SetValueAsync(RoundsCount, roundInfo);

            return null;
        }
        
        public async Task<object> GetTimeSlot(string accountAddress = null, ulong roundsCount = 0)
        {
            Interlocked.CompareExchange(ref accountAddress, null,
                AddressHashToString(Api.GetTransaction().From));
            
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount})).TimeSlot;
        }

        public async Task<object> GetInValueOf(string accountAddress, ulong roundsCount = 0)
        {
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount}))?.InValue;
        }
        
        public async Task<object> GetOutValueOf(string accountAddress, ulong roundsCount = 0)
        {
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount}))?.OutValue;
        }
        
        public async Task<object> GetSignatureOf(string accountAddress, ulong roundsCount = 0)
        {
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount}))?.Signature;
        }
        
        public async Task<object> GetOrderOf(string accountAddress, ulong roundsCount = 0)
        {
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount}))?.Order;
        }
        
        public async Task<Hash> CalculateSignature(Hash inValue)
        {
            var add = Hash.Default;
            var blockProducer = await GetBlockProducers();
            foreach (var node in blockProducer.Nodes)
            {
                var bpInfo = await GetBlockProducerInfoOfSpecificRound(node, RoundsCountMinusOne(RoundsCount));
                var lastSignature = bpInfo.Signature;
                add = add.CalculateHashWith(lastSignature);
            }
            
            return inValue.CalculateHashWith(add);
        }
        
        public async Task<object> AbleToMine(string accountAddress)
        {
            var assignedTimeSlot = (Timestamp) await GetTimeSlot(accountAddress);
            var timeSlotEnd = assignedTimeSlot.ToDateTime().AddSeconds(MiningTime).ToTimestamp();

            return CompareTimestamp(assignedTimeSlot, GetTimestamp()) 
                   && CompareTimestamp(timeSlotEnd, assignedTimeSlot);
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
        private async Task<object> IsBP(string accountAddress)
        {
            var blockProducer = (BlockProducer) await GetBlockProducers();
            return blockProducer.Nodes.Contains(accountAddress);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<object> IsEBP(string accountAddress)
        {
            var info = await GetBlockProducerInfoOfCurrentRound(accountAddress);
            return info.IsEBP;
        }
        
        public async Task<object> IsTimeToProduceExtraBlock()
        {
            var expectedTime = await _timeForProducingExtraBlock.GetAsync();
            return CompareTimestamp(GetTimestamp(), expectedTime)
                   && CompareTimestamp(expectedTime, GetTimestamp(MiningTime));
        }

        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();

            if (member != null) await (Task<object>) member.Invoke(this, parameters);
        }

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestamp(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(offset));
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private bool CompareTimestamp(Timestamp ts1, Timestamp ts2)
        {
            return ts1.ToDateTime() > ts2.ToDateTime();
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
        
        private async Task<BPInfo> GetBlockProducerInfoOfSpecificRound(Hash accountHash, UInt64Value roundsCount)
        {
            return (await _dPoSInfoMap.GetValueAsync(roundsCount)).Info[AddressHashToString(accountHash)];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(string accountAddress)
        {
            return (await _dPoSInfoMap.GetValueAsync(RoundsCount)).Info[accountAddress];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(Hash accountHash)
        {
            return (await _dPoSInfoMap.GetValueAsync(RoundsCount)).Info[AddressHashToString(accountHash)];
        }

        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().Value.ToBase64();
        }

        private Hash AddressStringToHash(string accountAddress)
        {
            return Convert.FromBase64String(accountAddress);
        }
    }
}