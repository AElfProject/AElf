using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Genesis
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class ContractZeroWithDPoS : BasicContractZero
    {
        #region Protobuf fields and maps

        private readonly UInt64Field _roundsCount = new UInt64Field(Globals.DPoSRoundsCountString);

        private readonly PbField<BlockProducer> _blockProducer =
            new PbField<BlockProducer>(Globals.DPoSBlockProducerString);

        private readonly Map<UInt64Value, RoundInfo> _dPoSInfoMap =
            new Map<UInt64Value, RoundInfo>(Globals.DPoSInfoString);
        
        // ReSharper disable once InconsistentNaming
        private readonly Map<UInt64Value, StringValue> _eBPMap =
            new Map<UInt64Value, StringValue>(Globals.DPoSExtraBlockProducerString);

        private readonly PbField<Timestamp> _timeForProducingExtraBlock =
            new PbField<Timestamp>(Globals.DPoSExtraBlockTimeslotString);

        private readonly PbField<Hash> _chainCreator = new PbField<Hash>(Globals.DPoSChainCreatorString);

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap
            = new Map<UInt64Value, StringValue>(Globals.DPoSFirstPlaceOfEachRoundString);

        #endregion

        private readonly bool _printLog = true;
 
        private UInt64Value RoundsCount => new UInt64Value {Value = _roundsCount.GetAsync().Result};
        private BlockProducer BlockProducer => _blockProducer.GetAsync().Result;
        
        #region Methods of Block Height 1
        
        // ReSharper disable once UnusedMember.Global
        public async Task InitializeConsensus(DPoSInfo dPoSInfo, BlockProducer blockProducer)
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
                GetTimestamp(dPoSInfo.RoundInfo[0].Info.Last().Value.TimeSlot, Globals.MiningTime));

            await _chainCreator.SetAsync(Api.GetTransaction().From);
        }
        
        #endregion

        #region Methods for Extra Block Producer

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public async Task SyncStateOfNextRound(RoundInfo suppliedPreviousRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            if (!Authentication(nameof(SyncStateOfNextRound)))
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
                Globals.MiningTime + Globals.CheckTime));

            //Update the rounds count at last
            await _roundsCount.SetAsync(RoundsCountAddOne(RoundsCount).Value);

            ConsoleWriteLine($"Sync dpos info of round {RoundsCount.Value} succeed");
        }

        #endregion

        #region Methods for Normal Block Producer
        
        // ReSharper disable once UnusedMember.Global
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
        
        // ReSharper disable once UnusedMember.Global
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

        #region Methods for Every node

        // ReSharper disable once UnusedMember.Global
        public async Task<BoolValue> BlockProducerVerification(StringValue accountAddress)
        {
            if (!IsBP(accountAddress.Value))
            {
                return new BoolValue {Value = false};
            }

            var now = GetTimestampOfUtcNow();
            var timeslotOfBlockProducer = await GetTimeSlot(accountAddress.Value);
            var endOfTimeslotOfBlockProducer = GetTimestamp(timeslotOfBlockProducer, Globals.MiningTime);
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
                var timeslotEnd = GetTimestamp(timeslot, Globals.MiningTime);
                if (CompareTimestamp(now, timeslot) && CompareTimestamp(timeslotEnd, now))
                {
                    return new BoolValue {Value = true};
                }
            }

            ConsoleWriteLine(accountAddress.Value + " may produced a block in an invalid timeslot:" + timeslotOfBlockProducer.ToDateTime().ToString("u"));

            return new BoolValue {Value = false};
        }

        #endregion
        
        #region Private Methods

        private async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }
        
        // ReSharper disable once InconsistentNaming
        private bool IsBP(string accountAddress)
        {
            return BlockProducer.Nodes.Contains(accountAddress);
        }
        
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        private async Task<bool> IsEBP(string accountAddress)
        {
            var info = await GetBlockProducerInfoOfCurrentRound(accountAddress);
            return info.IsEBP;
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

        // ReSharper disable once MemberCanBeMadeStatic.Local
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

        // ReSharper disable once UnusedParameter.Local
        private bool Authentication(string methodName)
        {
            var fromAccount = ConvertToNormalHexString(Api.GetTransaction().From.Value.ToByteArray().ToHex());
            var result = IsBP(fromAccount);
            //ConsoleWriteLine($"Checked privilege to call consensus method {methodName}: {result}");
            return result;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string ConvertToNormalHexString(string hexStr)
        {
            return hexStr.StartsWith("0x") ? hexStr.Remove(0, 2) : hexStr;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
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

    }
}