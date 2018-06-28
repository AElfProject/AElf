using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.KernelAccount;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Crypto.Engines;
using SharpRepository.Repository.Configuration;
using Api = AElf.Sdk.CSharp.Api;
using CSharpSmartContract = AElf.Sdk.CSharp.CSharpSmartContract;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Tests
{
    public class TestContractZero : CSharpSmartContract, ISmartContractZero
    {
        [SmartContractFieldData("${this}._lock", DataAccessMode.ReadWriteAccountSharing)]
        private object _lock;
        public override async Task InvokeAsync()
        {
            await Task.CompletedTask;
        }

        [SmartContractFunction("${this}.DeploySmartContract", new string[]{}, new string[]{"${this}._lock"})]
        public async Task<Hash> DeploySmartContract(int category, byte[] contract)
        {
            SmartContractRegistration registration = new SmartContractRegistration
            {
                Category = category,
                ContractBytes = ByteString.CopyFrom(contract),
                ContractHash = contract.CalculateHash() // maybe no usage  
            };
            
            var tx = Api.GetTransaction();
            
            // calculate new account address
            var account = Path.CalculateAccountAddress(tx.From, tx.IncrementId).ToAccount();
            
            await Api.DeployContractAsync(account, registration);
            Console.WriteLine("Deployment success, {0}", account.Value.ToBase64());
            return account.Value;
        }

        public void Print(string name)
        {
            Console.WriteLine("Hello, " + name);
        }

        #region DPoS

        private const int MiningTime = 8000;

        private const int WaitFirstRoundTime = 3000;

        private const int CheckTime = 3000;

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

            return blockProducer;
        }

        public async Task<BlockProducer> SetBlockProducers(BlockProducer blockProducers)
        {
            await _blockProducer.SetAsync(blockProducers);

            return blockProducers;
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
                bpInfo.TimeSlot = GetTimestamp(i * MiningTime + WaitFirstRoundTime);

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

                bpInfo.TimeSlot = GetTimestamp(i * MiningTime + addition + WaitFirstRoundTime);
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
        }
        
        #endregion

        #region EBP Methods

        public async Task<RoundInfo> GenerateNextRoundOrder()
        {
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
        
        public async Task<StringValue> SetNextExtraBlockProducer()
        {
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

        public async Task<UInt64Value> SetRoundsCount()
        {
            var newRoundsCount = RoundsCountAddOne(RoundsCount);
            await _roundsCount.SetAsync(newRoundsCount.Value);

            return newRoundsCount;
        }
        
        public async Task<UInt64Value> GetRoundsCount()
        {
            return new UInt64Value {Value = await _roundsCount.GetAsync()};
        }

        // ReSharper disable once InconsistentNaming
        public async Task SyncStateOfNextRound(RoundInfo suppliedPreviousRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            if (RoundsCount.Value != 1)
            {
                await _eBPMap.SetValueAsync(RoundsCountAddOne(RoundsCount), nextEBP);
                nextRoundInfo.Info.First(info => info.Key == nextEBP.Value).Value.IsEBP = true;
            }

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
        }

        #endregion

        public async Task<BoolValue> ReadyForHelpingProducingExtraBlock()
        {
            var me = Api.GetTransaction().From;
            var meOrder = (await GetBlockProducerInfoOfCurrentRound(AddressHashToString(me))).Order;
            // ReSharper disable once InconsistentNaming
            var currentEBP = await _eBPMap.GetValueAsync(RoundsCount);
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

            var now = GetTimestamp();

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
                Value = CompareTimestamp(now, assigendExtraBlockProducingTimeEndWithOffset)
                        && CompareTimestamp(GetTimestamp(assigendExtraBlockProducingTimeEndWithOffset, MiningTime), now)
            };
        }

        #region BP Methods

        public async Task<BPInfo> PublishOutValueAndSignature(Hash outValue, Hash signature, UInt64Value roundsCount)
        {
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
        
        public async Task<BPInfo> PublishOutValueAndSignatureDebug(string outValue, string signature, ulong roundsCount)
        {
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};

            var info = await GetBlockProducerInfoOfSpecificRound(accountAddress, count);
            
            info.OutValue = Base64StringToHash(outValue.Substring(2, outValue.Length - 2));
            if (roundsCount > 1)
                info.Signature = Base64StringToHash(signature.Substring(2, signature.Length - 2));
            
            var roundInfo = await _dPoSInfoMap.GetValueAsync(count);
            roundInfo.Info[accountAddress] = info;
            
            await _dPoSInfoMap.SetValueAsync(count, roundInfo);

            return info;
        }

        public async Task<Hash> TryToPublishInValue(Hash inValue, UInt64Value roundsCount)
        {
            var accountAddress = AddressHashToString(Api.GetTransaction().From);
            
            var info = await GetBlockProducerInfoOfSpecificRound(accountAddress, roundsCount);
            info.InValue = inValue;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundsCount);
            roundInfo.Info[accountAddress] = info;

            await _dPoSInfoMap.SetValueAsync(RoundsCount, roundInfo);

            return inValue;
        }

        /// <summary>
        /// Supplement of Round info.
        /// </summary>
        /// <returns></returns>
        public async Task<RoundInfo> SupplyPreviousRoundInfo()
        {
            var roundInfo = await _dPoSInfoMap.GetValueAsync(RoundsCount);

            foreach (var info in roundInfo.Info)
            {
                if (info.Value.InValue == null)
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

            await _dPoSInfoMap.SetValueAsync(RoundsCount, roundInfo);

            return roundInfo;
        }
        
        #endregion
        
        
        public async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }

        public async Task<Hash> GetInValueOf(string accountAddress, ulong roundsCount)
        {
            roundsCount = roundsCount == 0 ? RoundsCount.Value : roundsCount;
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress,
                new UInt64Value {Value = roundsCount}))?.InValue;
        }
        
        public async Task<Hash> GetOutValueOf(string accountAddress, ulong roundsCount)
        {
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.OutValue;
        }
        
        public async Task<Hash> GetSignatureOf(string accountAddress, ulong roundsCount)
        {
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.Signature;
        }
        
        public async Task<int?> GetOrderOf(string accountAddress, ulong roundsCount)
        {
            var count = roundsCount == 0 ? RoundsCount : new UInt64Value {Value = roundsCount};
            return (await GetBlockProducerInfoOfSpecificRound(accountAddress, count))?.Order;
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

            Hash sig = inValue.CalculateHashWith(add);
            return sig;
        }
        
        public async Task<bool> AbleToMine()
        {
            var accountHash = Api.GetTransaction().From;
            var accountAddress = AddressHashToString(accountHash);
            var now = GetTimestamp();

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
            var now = GetTimestamp();
            return CompareTimestamp(now, expectedTime)
                   && CompareTimestamp(GetTimestamp(expectedTime, MiningTime), now);
        }
        
        public async Task<bool> IsTimeToProduceExtraBlockDebug()
        {
            var expectedTime = await _timeForProducingExtraBlock.GetAsync();
            var now = GetTimestamp();
            return CompareTimestamp(GetTimestamp(), expectedTime)
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
                    = result + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime():u}\n"
                             + "Current Round : " + RoundsCount?.Value
            };
            
            return res;
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
                result += "Timeslot:\t" + bpInfo.Value.TimeSlot.ToDateTime().ToString("u") + "\n";
                result += "Signature:\t" + bpInfo.Value.Signature + "\n";
                result += "Out Value:\t" + bpInfo.Value.OutValue + "\n";
                result += "In Value:\t" + bpInfo.Value.InValue + "\n";
            }

            return result + "\n";
        }

        #region Private Methods

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestamp(int offset = 0)
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

        private Hash Base64StringToHash(string accountAddress)
        {
            return Convert.FromBase64String(accountAddress);
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
            if (m < 0)
            {
                m = -m;
            }

            return m;
        }

        #endregion

        #endregion
    }
}
