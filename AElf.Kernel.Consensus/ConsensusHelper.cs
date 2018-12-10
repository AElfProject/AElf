using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Managers;
using AElf.Configuration.Config.Consensus;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBeMadeStatic.Local
    // ReSharper disable UnusedMember.Global
    public class ConsensusHelper
    {
        private readonly IMinersManager _minersManager;
        private readonly ConsensusDataReader _reader;

        private readonly ILogger _logger = LogManager.GetLogger(nameof(ConsensusHelper));

        public List<string> Miners => _minersManager.GetMiners().Result.PublicKeys.ToList();

        public UInt64Value CurrentRoundNumber
        {
            get
            {
                try
                {
                    return UInt64Value.Parser.ParseFrom(
                        _reader.ReadFiled<UInt64Value>(GlobalConfig.AElfDPoSCurrentRoundNumber));
                }
                catch (Exception)
                {
                    return new UInt64Value {Value = 0};
                }
            }
        }

        private Round CurrentRoundInfo
        {
            get
            {
                try
                {
                    return Round.Parser.ParseFrom(_reader.ReadMap<Round>(CurrentRoundNumber,
                        GlobalConfig.AElfDPoSRoundsMapString));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get DPoS information of current round.\n");
                    return new Round();
                }
            }
        }

        private SInt32Value MiningInterval
        {
            get
            {
                try
                {
                    return SInt32Value.Parser.ParseFrom(
                        _reader.ReadFiled<SInt32Value>(GlobalConfig.AElfDPoSMiningIntervalString));
                }
                catch (Exception)
                {
                    return new SInt32Value {Value = ConsensusConfig.Instance.DPoSMiningInterval};
                }
            }
        }

        public ConsensusHelper(IMinersManager minersManager, ConsensusDataReader reader)
        {
            _minersManager = minersManager;
            _reader = reader;
        }

        /// <summary>
        /// Get block producer information of current round.
        /// </summary>
        /// <param name="accountAddressHex"></param>
        public MinerInRound this[string accountAddressHex]
        {
            get
            {
                try
                {
                    var bytes = _reader.ReadMap<Round>(CurrentRoundNumber, GlobalConfig.AElfDPoSRoundsMapString);
                    var round = Round.Parser.ParseFrom(bytes);
                    if (round.RealTimeMinersInfo.ContainsKey(accountAddressHex))
                        return round.RealTimeMinersInfo[accountAddressHex];

                    _logger.Error("No such Block Producer in current round.");
                    return default(MinerInRound);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to get Block Producer information of current round.");
                    return default(MinerInRound);
                }
            }
        }

        public MinerInRound this[byte[] pubKey] => this[pubKey.ToPlainBase58()];

        private Round this[UInt64Value roundNumber]
        {
            get
            {
                try
                {
                    var bytes = _reader.ReadMap<Round>(roundNumber, GlobalConfig.AElfDPoSRoundsMapString);
                    var round = Round.Parser.ParseFrom(bytes);
                    return round;
                }
                catch (Exception)
                {
                    return default(Round);
                }
            }
        }

        public StringValue GetDPoSInfoToString()
        {
            ulong count = 1;

            if (CurrentRoundNumber.Value != 0)
            {
                count = CurrentRoundNumber.Value;
            }

            var infoOfOneRound = "";

            ulong i = 1;
            while (i <= count)
            {
                var roundInfoStr = GetRoundInfoToString(new UInt64Value {Value = i});
                infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                i++;
            }

            var res = new StringValue
            {
                Value =
                    infoOfOneRound + "Current Round : " + CurrentRoundNumber?.Value
            };

            return res;
        }

        private string GetDPoSInfoToStringOfLatestRounds(ulong countOfRounds)
        {
            try
            {
                if (CurrentRoundNumber.Value == 0)
                {
                    return "Somehow current round number is 0";
                }

                if (countOfRounds == 0)
                {
                    return "";
                }

                var currentRoundNumber = CurrentRoundNumber.Value;
                ulong startRound;
                if (countOfRounds >= currentRoundNumber)
                {
                    startRound = 1;
                }
                else
                {
                    startRound = currentRoundNumber - countOfRounds + 1;
                }

                var infoOfOneRound = "";
                var i = startRound;
                while (i <= currentRoundNumber)
                {
                    if (i <= 0)
                    {
                        continue;
                    }

                    var roundInfoStr = GetRoundInfoToString(new UInt64Value {Value = i});
                    infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                    i++;
                }

                return
                    infoOfOneRound
                    + $"Current Round : {CurrentRoundNumber.Value}";
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to get dpos info");
                return "";
            }
        }

        /// <summary>
        /// This method should return true if all the BPs restarted (and missed their time slots).
        /// </summary>
        /// <returns></returns>
        public bool CanRecoverDPoSInformation()
        {
            return false;
        }

        public void SyncMiningInterval()
        {
            ConsensusConfig.Instance.DPoSMiningInterval = MiningInterval.Value;
            _logger?.Info($"Set AElf DPoS mining interval to: {GlobalConfig.AElfDPoSMiningInterval} ms.");
        }

        public void LogDPoSInformation(ulong height)
        {
            _logger?.Trace("Log dpos information - Start");
            _logger?.Trace(GetDPoSInfoToStringOfLatestRounds(GlobalConfig.AElfDPoSLogRoundCount) +
                           $". Current height: {height}");
            _logger?.Trace("Log dpos information - End");
        }

        public Round GetCurrentRoundInfo(UInt64Value currentRoundNumber = null)
        {
            if (currentRoundNumber == null)
            {
                currentRoundNumber = CurrentRoundNumber;
            }

            return currentRoundNumber.Value != 0 ? this[currentRoundNumber] : null;
        }

        public bool TryGetRoundInfo(ulong roundNumber, out Round roundInfo)
        {
            if (roundNumber == 0)
            {
                roundInfo = null;
                return false;
            }
            
            var info = this[roundNumber.ToUInt64Value()];
            if (info != null)
            {
                roundInfo = info;
                return true;
            }

            roundInfo = null;
            return false;
        }

        private string GetRoundInfoToString(UInt64Value roundNumber)
        {
            try
            {
                var result = "";

                var roundInfo = this[roundNumber];
                foreach (var minerInfo in roundInfo.RealTimeMinersInfo.OrderBy(m => m.Value.Order))
                {
                    result += GetAlias(minerInfo.Key) + (minerInfo.Value.IsExtraBlockProducer ? " [Current EBP]:\n" : ":\n");
                    result += "Order:\t\t" + minerInfo.Value.Order + "\n";
                    result += "Mining Time:\t" +
                              minerInfo.Value.ExpectedMiningTime.ToDateTime().ToLocalTime().ToString("u") + "\n";
                    result += "Signature:\t" +
                              minerInfo.Value.Signature?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "Out Value:\t" + minerInfo.Value.OutValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "In Value:\t" + minerInfo.Value.InValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "Mined Blocks:\t" + minerInfo.Value.ProducedBlocks + "\n";
                    result += "Is Forked:\t" + minerInfo.Value.IsForked + "\n";
                    result += "Missed Slots:\t" + minerInfo.Value.MissedTimeSlots + "\n";
                }

                return result + $"\nEBP TimeSlot of current round: {roundInfo.GetEBPMiningTime(MiningInterval.Value).ToLocalTime():u}\n";
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Failed to get dpos info of round {roundNumber.Value}");
                return "";
            }
        }

        private string GetAlias(string publicKey)
        {
            return StringValue.Parser.ParseFrom(_reader.ReadMap<StringValue>(new StringValue {Value = publicKey},
                GlobalConfig.AElfDPoSAliasesMapString)).Value;
        }
    }
}