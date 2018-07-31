using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Consensus;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis.ConsensusContract
{
    // ReSharper disable once InconsistentNaming
    public class AElfDPoS : IConsensus
    {
        public ConsensusType Type => ConsensusType.AElfDPoS;

        public ulong CurrentRoundNumber => _currentRoundNumberField.GetAsync().Result;

        public int Interval => Globals.AElfMiningTime;

        public bool PrintLogs => true;

        public Hash Nonce { get; set; } = Hash.Default;

        #region Protobuf fields and maps
        
        private readonly UInt64Field _currentRoundNumberField;

        private readonly PbField<BlockProducer> _blockProducerField;

        private readonly Map<UInt64Value, RoundInfo> _dPoSInfoMap;
        
        // ReSharper disable once InconsistentNaming
        private readonly Map<UInt64Value, StringValue> _eBPMap;

        private readonly PbField<Timestamp> _timeForProducingExtraBlockField;

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap;

        #endregion

        public AElfDPoS(AElfDPoSFiledMapCollection collection)
        {
            _currentRoundNumberField = collection.CurrentRoundNumberField;
            _blockProducerField = collection.BlockProducerField;
            _dPoSInfoMap = collection.DPoSInfoMap;
            _eBPMap = collection.EBPMap;
            _timeForProducingExtraBlockField = collection.TimeForProducingExtraBlockField;
            _firstPlaceMap = collection.FirstPlaceMap;
        }

        /// <inheritdoc />
        /// <summary>
        /// 1. Set block producers;
        /// 2. Set current round number to 1;
        /// 3. Set first place of round 1 and 2 using DPoSInfo;
        /// 4. Set DPoS information of first round to map;
        /// 5. Set EBP of round 1 and 2;
        /// 6. Set Extra Block mining timeslot of current round (actually round 1).
        /// </summary>
        /// <param name="args">
        /// 2 args:
        /// [0] BlockProducer
        /// [1] DPoSInfo
        /// </param>
        /// <returns></returns>
        public async Task Initialize(List<byte[]> args)
        {
            if (args.Count != 2)
            {
                return;
            }
            
            var round1 = new UInt64Value {Value = 1};
            var round2 = new UInt64Value {Value = 2};
            BlockProducer blockProducer;
            DPoSInfo dPoSInfo;
            try
            {
                blockProducer = BlockProducer.Parser.ParseFrom(args[0]);
                dPoSInfo = DPoSInfo.Parser.ParseFrom(args[1]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to parse from byte array.", e);
                return;
            }
            
            // 1. Set block producers;
            try
            {
                await InitializeBlockProducer(blockProducer);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set block producers.", e);
            }
            
            // 2. Set current round number to 1;
            try
            {
                await UpdateCurrentRoundNumber(1);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to update current round number.", e);
            }
            
            // 3. Set first place of round 1 and 2 using DPoSInfo;
            try
            {
                await SetFirstPlaceOfSpecificRound(round1, dPoSInfo);
                await SetFirstPlaceOfSpecificRound(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set first place.", e);
            }

            // 4. Set DPoS information of first round to map;
            try
            {
                await SetDPoSInfoToMap(round1, dPoSInfo);
                await SetDPoSInfoToMap(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set DPoS information of first round to map.", e);
            }

            // 5. Set EBP of round 1 and 2;
            try
            {
                await SetExtraBlockProducerOfSpecificRound(round1, dPoSInfo);
                await SetExtraBlockProducerOfSpecificRound(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set Extra Block Producer.", e);
            }

            // 6. Set Extra Block mining timeslot of current round (actually round 1);
            try
            {
                await SetExtraBlockMiningTimeslotOfSpecificRound(round1, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set Extra Block mining timeslot.", e);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// 1. Supply DPoS information of current round (in case of some block producers failed to
        ///     publish their out value, signature or in value);
        /// 2. Set DPoS information of next round.
        /// </summary>
        /// <param name="args">
        /// 3 args:
        /// [0] RoundInfo
        /// [1] RoundInfo
        /// [2] StringValue
        /// </param>
        /// <returns></returns>
        public async Task Update(List<byte[]> args)
        {
            if (args.Count != 3)
            {
                return;
            }
            
            RoundInfo currentRoundInfo;
            RoundInfo nextRoundInfo;
            StringValue nextExtraBlockProducer;
            try
            {
                currentRoundInfo = RoundInfo.Parser.ParseFrom(args[0]);
                nextRoundInfo = RoundInfo.Parser.ParseFrom(args[1]);
                nextExtraBlockProducer = StringValue.Parser.ParseFrom(args[2]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to parse from byte array.", e);
                return;
            }

            await SupplyDPoSInformationOfCurrentRound(currentRoundInfo);
            await SetDPoSInformationOfNextRound(nextRoundInfo, nextExtraBlockProducer);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="args">
        /// (I) Publish out value and signature
        /// 4 args:
        /// [0] UInt64Value
        /// [1] StringValue
        /// [2] Hash
        /// [3] Hash
        /// 
        /// (II) Publish in value
        /// 3 args:
        /// [0] UInt64Value
        /// [1] StringValue
        /// [2] Hash
        /// </param>
        /// <returns></returns>
        public async Task Publish(List<byte[]> args)
        {
            if (args.Count < 3)
            {
                return;
            }
            
            UInt64Value roundNumber;
            StringValue accountAddress;
            try
            {
                roundNumber = UInt64Value.Parser.ParseFrom(args[0]);
                accountAddress = StringValue.Parser.ParseFrom(args[1]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Publish), "Failed to parse from byte array.", e);
                return;
            }
            
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (args.Count == 4)
            {
                Hash outValue;
                Hash signature;

                try
                {
                    outValue = Hash.Parser.ParseFrom(args[2]);
                    signature = Hash.Parser.ParseFrom(args[3]);
                }
                catch (Exception e)
                {
                    ConsoleWriteLine(nameof(Publish), "Failed to parse from byte array (Hash).", e);
                    return;
                }

                await PublishOutValueAndSignature(roundNumber, accountAddress, outValue, signature);
            }

            if (args.Count == 3)
            {
                Hash inValue;

                try
                {
                    inValue = Hash.Parser.ParseFrom(args[2]);
                }
                catch (Exception e)
                {
                    ConsoleWriteLine(nameof(Publish), "Failed to parse from byte array (Hash).", e);
                    return;
                }

                await PublishInValue(roundNumber, accountAddress, inValue);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Checking steps:
        /// 1. Contained by BlockProducer.Nodes;
        /// 2. Timestamp sitting in correct timeslot of current round, or later than extra block timeslot
        ///     if Extra Block Producer failed to produce extra block.
        /// </summary>
        /// <param name="args">
        /// 2 args:
        /// [0] StringValue
        /// [1] Timestamp
        /// </param>
        /// <returns></returns>
        public async Task<bool> Validation(List<byte[]> args)
        {
            StringValue accountAddress;
            Timestamp timestamp;
            try
            {
                accountAddress = StringValue.Parser.ParseFrom(args[0]);
                timestamp = Timestamp.Parser.ParseFrom(args[1]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Validation), "Failed to parse from byte array.", e);
                return false;
            }

            // 1. Contained by BlockProducer.Nodes;
            if (!IsBlockProducer(accountAddress))
            {
                return false;
            }

            // 2. Timestamp sitting in correct timeslot of current round;
            var timeslotOfBlockProducer = (await GetBPInfoOfCurrentRound(accountAddress)).TimeSlot;
            var endOfTimeslotOfBlockProducer = GetTimestampWithOffset(timeslotOfBlockProducer, Interval);
            // ReSharper disable once InconsistentNaming
            var timeslotOfEBP = await _timeForProducingExtraBlockField.GetAsync();
            return CompareTimestamp(timestamp, timeslotOfBlockProducer) && CompareTimestamp(endOfTimeslotOfBlockProducer, timestamp) ||
                   CompareTimestamp(timestamp, timeslotOfEBP);
        }

        #region Private Methods

        #region Important Privite Methods

        private async Task InitializeBlockProducer(BlockProducer blockProducer)
        {
            foreach (var bp in blockProducer.Nodes)
            {
                ConsoleWriteLine(nameof(Initialize), $"Set Block Producer: {bp}");
            }
            await _blockProducerField.SetAsync(blockProducer);
        }

        private async Task UpdateCurrentRoundNumber(ulong currentRoundNumber)
        {
            await _currentRoundNumberField.SetAsync(currentRoundNumber);
        }

        private async Task SetFirstPlaceOfSpecificRound(UInt64Value roundNumber, DPoSInfo info)
        {
            await _firstPlaceMap.SetValueAsync(roundNumber,
                new StringValue {Value = info.GetRoundInfo(roundNumber.Value).Info.First().Key});
        }
        
        private async Task SetFirstPlaceOfSpecificRound(UInt64Value roundNumber, StringValue accountAddress)
        {
            await _firstPlaceMap.SetValueAsync(roundNumber, accountAddress);
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetDPoSInfoToMap(UInt64Value roundNumber, DPoSInfo info)
        {
            await _dPoSInfoMap.SetValueAsync(roundNumber, info.GetRoundInfo(roundNumber.Value));
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task SetDPoSInfoToMap(UInt64Value roundNumber, RoundInfo info)
        {
            await _dPoSInfoMap.SetValueAsync(roundNumber, info);
        }

        private async Task SetExtraBlockProducerOfSpecificRound(UInt64Value roundNumber, DPoSInfo info)
        {
            await _eBPMap.SetValueAsync(roundNumber, info.GetExtraBlockProducerOfSpecificRound(roundNumber.Value));
        }

        private async Task SetExtraBlockProducerOfSpecificRound(UInt64Value roundNumber, StringValue extraBlockProducer)
        {
            await _eBPMap.SetValueAsync(roundNumber, extraBlockProducer);
        }

        private async Task SetExtraBlockMiningTimeslotOfSpecificRound(UInt64Value roundNumber, DPoSInfo info)
        {
            await _timeForProducingExtraBlockField.SetAsync(GetTimestampWithOffset(
                info.GetLastBlockProducerTimeslotOfSpecificRound(roundNumber.Value), Interval));
        }
        
        private async Task SetExtraBlockMiningTimeslotOfSpecificRound(Timestamp timestamp)
        {
            await _timeForProducingExtraBlockField.SetAsync(timestamp);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task SupplyDPoSInformationOfCurrentRound(RoundInfo currentRoundInfo)
        {
            // ReSharper disable once InconsistentNaming
            var currentRoundInfoFromDPoSMap = new RoundInfo();

            try
            {
                currentRoundInfoFromDPoSMap = await GetCurrentRoundInfo();
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to get current RoundInfo.", e);
            }

            try
            {
                foreach (var infoPair in currentRoundInfoFromDPoSMap.Info)
                {
                    //If one Block Producer failed to pulish his in value (with a tx),
                    //it means maybe something wrong happened to him.
                    if (infoPair.Value.InValue != null && infoPair.Value.OutValue != null) 
                        continue;
                
                    //So the Extra Block Producer of this round will help him to supply all the needed information
                    //which contains in value, out value, signature.
                    var supplyValue = currentRoundInfo.Info.First(info => info.Key == infoPair.Key)
                        .Value;
                    infoPair.Value.InValue = supplyValue.InValue;
                    infoPair.Value.OutValue = supplyValue.OutValue;
                    infoPair.Value.Signature = supplyValue.Signature;
                }
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to supply current RoundInfo", e);
                
                ConsoleWriteLine(nameof(Update), "Current RoundInfo:");

                foreach (var key in currentRoundInfo.Info.Keys)
                {
                    ConsoleWriteLine(nameof(Update), key);
                }
            }

            await SetCurrentRoundInfo(currentRoundInfoFromDPoSMap);
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetDPoSInformationOfNextRound(RoundInfo nextRoundInfo, StringValue nextExtraBlockProducer)
        {
            //Update Current Round Number.
            await UpdateCurrentRoundNumber();

            var newRoundNumber = new UInt64Value {Value = CurrentRoundNumber};
            
            //Update ExtraBlockProducer.
            await SetExtraBlockProducerOfSpecificRound(newRoundNumber, nextExtraBlockProducer);

            //Update RoundInfo.
            nextRoundInfo.Info.First(info => info.Key == nextExtraBlockProducer.Value).Value.IsEBP = true;

            //Update DPoSInfo.
            await SetDPoSInfoToMap(newRoundNumber, nextRoundInfo);

            //Update First Place.
            await SetFirstPlaceOfSpecificRound(newRoundNumber, new StringValue {Value = nextRoundInfo.Info.First().Key});
            
            //Update Extra Block Timeslot.
            await SetExtraBlockMiningTimeslotOfSpecificRound(GetTimestampWithOffset(
                nextRoundInfo.Info.Last().Value.TimeSlot, Interval + Globals.AElfCheckTime));

            ConsoleWriteLine(nameof(Update), $"Sync dpos info of round {CurrentRoundNumber} succeed");
        }

        private async Task<RoundInfo> GetCurrentRoundInfo()
        {
            return await _dPoSInfoMap.GetValueAsync(new UInt64Value {Value = CurrentRoundNumber});
        }

        private async Task SetCurrentRoundInfo(RoundInfo currentRoundInfo)
        {
            await _dPoSInfoMap.SetValueAsync(new UInt64Value {Value = CurrentRoundNumber}, currentRoundInfo);
        }

        private async Task UpdateCurrentRoundNumber()
        {
            await _currentRoundNumberField.SetAsync(CurrentRoundNumber + 1);
        }
        
        // ReSharper disable once UnusedMember.Global
        private async Task PublishOutValueAndSignature(UInt64Value roundNumber, StringValue accountAddress, Hash outValue, Hash signature)
        {
            var info = await GetBPInfoOfSpecificRound(accountAddress, roundNumber);

            info.OutValue = outValue;
            if (roundNumber.Value > 1)
                info.Signature = signature;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundNumber);
            roundInfo.Info[accountAddress.Value] = info;
            await _dPoSInfoMap.SetValueAsync(roundNumber, roundInfo);
        }

        // ReSharper disable once UnusedMember.Global
        private async Task PublishInValue(UInt64Value roundNumber, StringValue accountAddress, Hash inValue)
        {
            var info = await GetBPInfoOfSpecificRound(accountAddress, roundNumber);
            info.InValue = inValue;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundNumber);
            roundInfo.Info[accountAddress.Value] = info;

            await _dPoSInfoMap.SetValueAsync(roundNumber, roundInfo);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<BPInfo> GetBPInfoOfSpecificRound(StringValue accountAddress, UInt64Value roundNumber)
        {
            return (await _dPoSInfoMap.GetValueAsync(roundNumber)).Info[accountAddress.Value];
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<BPInfo> GetBPInfoOfCurrentRound(StringValue accountAddress)
        {
            return (await _dPoSInfoMap.GetValueAsync(new UInt64Value {Value = CurrentRoundNumber})).Info[accountAddress.Value];
        }

        private bool IsBlockProducer(StringValue accountAddress)
        {
            var blockProducer = _blockProducerField.GetValue();
            return blockProducer.Nodes.Contains(accountAddress.Value);
        }
        
        #endregion

        #region Utilities

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private DateTime GetLocalTime()
        {
            return DateTime.UtcNow.ToLocalTime();
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }
        
        private void ConsoleWriteLine(string prefix, string log, Exception ex = null)
        {
            if (!PrintLogs) 
                return;
            
            Console.WriteLine($"[{GetLocalTime():HH:mm:ss} - AElfDPoS]{prefix} - {log}");
            if (ex != null)
            {
                Console.WriteLine(ex);
            }
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

        #endregion

        #endregion
    }
}