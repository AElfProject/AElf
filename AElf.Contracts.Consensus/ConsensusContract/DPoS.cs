using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.ConsensusContract.FieldMapCollections;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.ConsensusContract
{
    // ReSharper disable InconsistentNaming
    public class DPoS : IConsensus
    {
        public ConsensusType Type => ConsensusType.AElfDPoS;

        public ulong CurrentRoundNumber => _currentRoundNumberField.GetAsync().Result;

        public int Interval
        {
            get
            {
                var interval = _miningIntervalField.GetAsync().Result;
                return interval == 0 ? 4000 : interval;
            }
        }

        public bool PrintLogs => true;

        public Hash Nonce { get; set; } = Hash.Default;

        #region Protobuf fields and maps
        
        private readonly UInt64Field _currentRoundNumberField;

        private readonly PbField<Miners> _blockProducerField;

        private readonly Map<UInt64Value, Round> _dPoSInfoMap;
        
        // ReSharper disable once InconsistentNaming
        private readonly Map<UInt64Value, StringValue> _eBPMap;

        private readonly PbField<Timestamp> _timeForProducingExtraBlockField;

        private readonly Map<UInt64Value, StringValue> _firstPlaceMap;

        private readonly Int32Field _miningIntervalField;

        #endregion

        public DPoS(AElfDPoSFiledMapCollection collection)
        {
            _currentRoundNumberField = collection.CurrentRoundNumberField;
            _blockProducerField = collection.BlockProducerField;
            _dPoSInfoMap = collection.DPoSInfoMap;
            _eBPMap = collection.EBPMap;
            _timeForProducingExtraBlockField = collection.TimeForProducingExtraBlockField;
            _firstPlaceMap = collection.FirstPlaceMap;
            _miningIntervalField = collection.MiningIntervalField;
        }

        /// <inheritdoc />
        /// <summary>
        /// 1. Set block producers / miners;
        /// 2. Set current round number to 1;
        /// 3. Set mining interval;
        /// 4. Set first place of round 1 and 2 using AElfDPoSInformation;
        /// 5. Set DPoS information of first round to map;
        /// 6. Set EBP of round 1 and 2;
        /// 7. Set Extra Block mining timeslot of current round (actually round 1).
        /// </summary>
        /// <param name="args">
        /// 3 args:
        /// [0] Miners
        /// [1] AElfDPoSInformation
        /// [2] SInt32Value
        /// </param>
        /// <returns></returns>
        public async Task Initialize(List<byte[]> args)
        {
            if (args.Count != 3)
            {
                return;
            }
            
            var round1 = new UInt64Value {Value = 1};
            var round2 = new UInt64Value {Value = 2};
            Miners miners;
            AElfDPoSInformation dPoSInfo;
            SInt32Value miningInterval;
            try
            {
                miners = Miners.Parser.ParseFrom(args[0]);
                dPoSInfo = AElfDPoSInformation.Parser.ParseFrom(args[1]);
                miningInterval = SInt32Value.Parser.ParseFrom(args[2]);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to parse from byte array.", e);
                return;
            }
            
            // 1. Set block producers;
            try
            {
                await InitializeBlockProducer(miners);
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
            
            // 3. Set mining interval;
            try
            {
                await SetMiningInterval(miningInterval);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set mining interval.", e);
            }
            
            // 4. Set first place of round 1 and 2 using DPoSInfo;
            try
            {
                await SetFirstPlaceOfSpecificRound(round1, dPoSInfo);
                await SetFirstPlaceOfSpecificRound(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set first place.", e);
            }

            // 5. Set DPoS information of first round to map;
            try
            {
                await SetDPoSInfoToMap(round1, dPoSInfo);
                await SetDPoSInfoToMap(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set DPoS information of first round to map.", e);
            }

            // 6. Set EBP of round 1 and 2;
            try
            {
                await SetExtraBlockProducerOfSpecificRound(round1, dPoSInfo);
                await SetExtraBlockProducerOfSpecificRound(round2, dPoSInfo);
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Initialize), "Failed to set Extra Block Producer.", e);
            }

            // 7. Set Extra Block mining timeslot of current round (actually round 1);
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
        /// [0] Round
        /// [1] Round
        /// [2] StringValue
        /// </param>
        /// <returns></returns>
        public async Task Update(List<byte[]> args)
        {
            if (args.Count != 3)
            {
                return;
            }
            
            Round currentRoundInfo;
            Round nextRoundInfo;
            StringValue nextExtraBlockProducer;
            try
            {
                currentRoundInfo = Round.Parser.ParseFrom(args[0]);
                nextRoundInfo = Round.Parser.ParseFrom(args[1]);
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
            var timeslotOfEBP = await _timeForProducingExtraBlockField.GetAsync();
            return CompareTimestamp(timestamp, timeslotOfBlockProducer) && CompareTimestamp(endOfTimeslotOfBlockProducer, timestamp) ||
                   CompareTimestamp(timestamp, timeslotOfEBP);
        }

        #region Private Methods

        #region Important Privite Methods

        private async Task InitializeBlockProducer(Miners miners)
        {
            foreach (var bp in miners.Nodes)
            {
                ConsoleWriteLine(nameof(Initialize), $"Set Miner: {bp}");
            }
            await _blockProducerField.SetAsync(miners);
        }

        private async Task UpdateCurrentRoundNumber(ulong currentRoundNumber)
        {
            await _currentRoundNumberField.SetAsync(currentRoundNumber);
        }

        private async Task SetMiningInterval(SInt32Value interval)
        {
            await _miningIntervalField.SetAsync(interval.Value);
        }

        private async Task SetFirstPlaceOfSpecificRound(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            await _firstPlaceMap.SetValueToDatabaseAsync(roundNumber,
                new StringValue {Value = info.GetRoundInfo(roundNumber.Value).BlockProducers.First().Key});
        }
        
        private async Task SetFirstPlaceOfSpecificRound(UInt64Value roundNumber, StringValue accountAddress)
        {
            await _firstPlaceMap.SetValueToDatabaseAsync(roundNumber, accountAddress);
        }

        private async Task SetDPoSInfoToMap(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            await _dPoSInfoMap.SetValueToDatabaseAsync(roundNumber, info.GetRoundInfo(roundNumber.Value));
        }
        
        private async Task SetDPoSInfoToMap(UInt64Value roundNumber, Round roundInfo)
        {
            await _dPoSInfoMap.SetValueToDatabaseAsync(roundNumber, roundInfo);
        }

        private async Task SetExtraBlockProducerOfSpecificRound(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            await _eBPMap.SetValueToDatabaseAsync(roundNumber, info.GetExtraBlockProducerOfSpecificRound(roundNumber.Value));
        }

        private async Task SetExtraBlockProducerOfSpecificRound(UInt64Value roundNumber, StringValue extraBlockProducer)
        {
            await _eBPMap.SetValueToDatabaseAsync(roundNumber, extraBlockProducer);
        }

        private async Task SetExtraBlockMiningTimeslotOfSpecificRound(UInt64Value roundNumber, AElfDPoSInformation info)
        {
            var lastMinerTimeslot = info.GetLastBlockProducerTimeslotOfSpecificRound(roundNumber.Value);
            var timeslot = GetTimestampWithOffset(lastMinerTimeslot, Interval);
            await _timeForProducingExtraBlockField.SetAsync(timeslot);
        }
        
        private async Task SetExtraBlockMiningTimeslotOfSpecificRound(Timestamp timestamp)
        {
            await _timeForProducingExtraBlockField.SetAsync(timestamp);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task SupplyDPoSInformationOfCurrentRound(Round currentRoundInfo)
        {
            // ReSharper disable once InconsistentNaming
            var currentRoundInfoFromDPoSMap = new Round();

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
                foreach (var infoPair in currentRoundInfoFromDPoSMap.BlockProducers)
                {
                    //If one Block Producer failed to pulish his in value (with a tx),
                    //it means maybe something wrong happened to him.
                    if (infoPair.Value.InValue != null && infoPair.Value.OutValue != null) 
                        continue;
                
                    //So the Extra Block Producer of this round will help him to supply all the needed information
                    //which contains in value, out value, signature.
                    var supplyValue = currentRoundInfo.BlockProducers.First(info => info.Key == infoPair.Key)
                        .Value;
                    infoPair.Value.InValue = supplyValue.InValue;
                    infoPair.Value.OutValue = supplyValue.OutValue;
                    infoPair.Value.Signature = supplyValue.Signature;
                }
            }
            catch (Exception e)
            {
                ConsoleWriteLine(nameof(Update), "Failed to supply information of current round.", e);
                
                ConsoleWriteLine(nameof(Update), "Current RoundInfo:");

                foreach (var key in currentRoundInfo.BlockProducers.Keys)
                {
                    ConsoleWriteLine(nameof(Update), key);
                }
            }

            await SetCurrentRoundInfo(currentRoundInfoFromDPoSMap);
        }

        // ReSharper disable once InconsistentNaming
        private async Task SetDPoSInformationOfNextRound(Round nextRoundInfo, StringValue nextExtraBlockProducer)
        {
            //Update Current Round Number.
            await UpdateCurrentRoundNumber();

            var newRoundNumber = new UInt64Value {Value = CurrentRoundNumber};
            
            //Update ExtraBlockProducer.
            await SetExtraBlockProducerOfSpecificRound(newRoundNumber, nextExtraBlockProducer);

            //Update RoundInfo.
            nextRoundInfo.BlockProducers.First(info => info.Key == nextExtraBlockProducer.Value).Value.IsEBP = true;

            //Update DPoSInfo.
            await SetDPoSInfoToMap(newRoundNumber, nextRoundInfo);

            //Update First Place.
            await SetFirstPlaceOfSpecificRound(newRoundNumber, new StringValue {Value = nextRoundInfo.BlockProducers.First().Key});
            
            //Update Extra Block Timeslot.
            await SetExtraBlockMiningTimeslotOfSpecificRound(GetTimestampWithOffset(
                nextRoundInfo.BlockProducers.Last().Value.TimeSlot, Interval));

            ConsoleWriteLine(nameof(Update), $"Sync dpos info of round {CurrentRoundNumber} succeed");
        }

        private async Task<Round> GetCurrentRoundInfo()
        {
            return await _dPoSInfoMap.GetValueAsync(new UInt64Value {Value = CurrentRoundNumber});
        }

        private async Task SetCurrentRoundInfo(Round currentRoundInfo)
        {
            await _dPoSInfoMap.SetValueToDatabaseAsync(new UInt64Value {Value = CurrentRoundNumber}, currentRoundInfo);
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
            roundInfo.BlockProducers[accountAddress.Value] = info;

            await _dPoSInfoMap.SetValueToDatabaseAsync(roundNumber, roundInfo);
        }

        // ReSharper disable once UnusedMember.Global
        private async Task PublishInValue(UInt64Value roundNumber, StringValue accountAddress, Hash inValue)
        {
            var info = await GetBPInfoOfSpecificRound(accountAddress, roundNumber);
            info.InValue = inValue;

            var roundInfo = await _dPoSInfoMap.GetValueAsync(roundNumber);
            roundInfo.BlockProducers[accountAddress.Value] = info;

            await _dPoSInfoMap.SetValueToDatabaseAsync(roundNumber, roundInfo);
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<BlockProducer> GetBPInfoOfSpecificRound(StringValue accountAddress, UInt64Value roundNumber)
        {
            return (await _dPoSInfoMap.GetValueAsync(roundNumber)).BlockProducers[accountAddress.Value];
        }
        
        // ReSharper disable once InconsistentNaming
        private async Task<BlockProducer> GetBPInfoOfCurrentRound(StringValue accountAddress)
        {
            return (await _dPoSInfoMap.GetValueAsync(new UInt64Value {Value = CurrentRoundNumber})).BlockProducers[accountAddress.Value];
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