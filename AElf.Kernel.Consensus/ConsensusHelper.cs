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
                    //_logger?.Error(e, "Failed to get DPoS mining interval.\n");
                    return new SInt32Value {Value = ConsensusConfig.Instance.DPoSMiningInterval};
                }
            }
        }

        private StringValue FirstPlaceBlockProducerOfCurrentRound
        {
            get
            {
                try
                {
                    return StringValue.Parser.ParseFrom(_reader.ReadMap<StringValue>(CurrentRoundNumber,
                        GlobalConfig.AElfDPoSFirstPlaceOfEachRoundString));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get first order prodocuer of current round.");
                    return new StringValue {Value = ""};
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
                catch (Exception e)
                {
                    _logger.Error(e,
                        $"Failed to get Round information of provided round number. - {roundNumber.Value}\n");
                    return default(Round);
                }
            }
        }

        public Term GenerateInfoForFirstTwoRounds()
        {
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var miner in Miners)
            {
                dict.Add(miner, miner[0]);
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();

            var infosOfRound1 = new Round();

            var selected = Miners.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new MinerInRound {IsExtraBlockProducer = false};

                if (i == selected)
                {
                    bpInfo.IsExtraBlockProducer = true;
                }

                bpInfo.Order = i + 1;
                bpInfo.Signature = Hash.Generate();
                bpInfo.ExpectedMiningTime =
                    GetTimestampOfUtcNow(i * ConsensusConfig.Instance.DPoSMiningInterval +
                                         GlobalConfig.AElfWaitFirstRoundTime);

                infosOfRound1.RealTimeMinersInfo.Add(enumerable[i], bpInfo);
            }

            // Second round
            dict = new Dictionary<string, int>();

            foreach (var miner in Miners)
            {
                dict.Add(miner, miner[0]);
            }

            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            enumerable = sortedMiningNodes.ToList();

            var infosOfRound2 = new Round();

            var addition = enumerable.Count * ConsensusConfig.Instance.DPoSMiningInterval +
                           ConsensusConfig.Instance.DPoSMiningInterval;

            selected = Miners.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new MinerInRound {IsExtraBlockProducer = false};

                if (i == selected)
                {
                    bpInfo.IsExtraBlockProducer = true;
                }

                bpInfo.ExpectedMiningTime = GetTimestampOfUtcNow(i * ConsensusConfig.Instance.DPoSMiningInterval +
                                                               addition +
                                                               GlobalConfig.AElfWaitFirstRoundTime);
                bpInfo.Order = i + 1;

                infosOfRound2.RealTimeMinersInfo.Add(enumerable[i], bpInfo);
            }

            infosOfRound1.RoundNumber = 1;
            infosOfRound2.RoundNumber = 2;

            var term = new Term
            {
                FirstRound = infosOfRound1,
                SecondRound = infosOfRound2,
                Miners = new Miners
                {
                    TakeEffectRoundNumber = 2,
                    PublicKeys = {Miners}
                },
                MiningInterval = ConsensusConfig.Instance.DPoSMiningInterval
            };

            return term;
        }
/*

        private Round SupplyPreviousRoundInfo()
        {
            try
            {
                var roundInfo = CurrentRoundInfo;

                foreach (var info in roundInfo.BlockProducers)
                {
                    if (info.Value.InValue != null && info.Value.OutValue != null) continue;

                    var inValue = Hash.Generate();
                    var outValue = Hash.FromMessage(inValue);

                    info.Value.OutValue = outValue;
                    info.Value.InValue = inValue;

                    //For the first round, the sig value is auto generated
                    if (info.Value.Signature == null && CurrentRoundNumber.Value != 1)
                    {
                        var signature = CalculateSignature(inValue);
                        info.Value.Signature = signature;
                    }

                    roundInfo.BlockProducers[info.Key] = info.Value;
                }

                roundInfo.RoundNumber = CurrentRoundNumber.Value;

                return roundInfo;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to supply previous round info");
                return new Round();
            }
        }

        public Hash CalculateSignature(Hash inValue)
        {
            try
            {
                var add = Hash.Default;
                foreach (var miner in Miners)
                {
                    var lastSignature = this[RoundNumberMinusOne(CurrentRoundNumber)].BlockProducers[miner.ToPlainBase58()].Signature;
                    add = Hash.FromTwoHashes(add, lastSignature);
                }

                var sig = Hash.FromTwoHashes(inValue, add);
                return sig;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to calculate signature");
                return Hash.Default;
            }
        }

        private Round GenerateNextRoundOrder()
        {
            try
            {
                var infosOfNextRound = new Round();
                var signatureDict = new Dictionary<Hash, string>();
                var orderDict = new Dictionary<int, string>();

                var blockProducerCount = Miners.Count;

                foreach (var miner in Miners)
                {
                    var s = this[miner].Signature;
                    if (s == null)
                    {
                        s = Hash.Generate();
                    }

                    signatureDict[s] = miner.ToPlainBase58();
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

                var blockTimeSlot = ExtraBlockTimeSlot;

                // Maybe because something happened with setting extra block time slot.
                if (blockTimeSlot.ToDateTime().AddMilliseconds(ConsensusConfig.Instance.DPoSMiningInterval * 1.5) <
                    GetTimestampOfUtcNow().ToDateTime())
                {
                    blockTimeSlot = GetTimestampOfUtcNow();
                }

                for (var i = 0; i < orderDict.Count; i++)
                {
                    var bpInfoNew = new BlockProducer
                    {
                        TimeSlot = GetTimestampWithOffset(blockTimeSlot,
                            i * ConsensusConfig.Instance.DPoSMiningInterval + ConsensusConfig.Instance.DPoSMiningInterval * 2),
                        Order = i + 1
                    };

                    infosOfNextRound.BlockProducers[orderDict[i]] = bpInfoNew;
                }

                infosOfNextRound.RoundNumber = CurrentRoundNumber.Value + 1;

                return infosOfNextRound;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to generate info of next round");
                return new Round();
            }
        }

        private StringValue CalculateNextExtraBlockProducer()
        {
            try
            {
                var firstPlace = FirstPlaceBlockProducerOfCurrentRound;
                var firstPlaceInfo = this[firstPlace.Value];
                var sig = firstPlaceInfo.Signature;
                if (sig == null)
                {
                    sig = Hash.Generate();
                }

                var sigNum = BitConverter.ToUInt64(
                    BitConverter.IsLittleEndian ? sig.Value.Reverse().ToArray() : sig.Value.ToArray(), 0);
                var blockProducerCount = Miners.Count;
                var order = GetModulus(sigNum, blockProducerCount);

                var nextEBP = Miners[order];

                return new StringValue {Value = nextEBP.ToPlainBase58().RemoveHexPrefix()};
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to set next extra block producer");
                return new StringValue {Value = ""};
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
                Value
                    = infoOfOneRound + $"EBP Time Slot of current round: {ExtraBlockTimeSlot.ToDateTime().ToLocalTime():u}\n"
                                     + "Current Round : " + CurrentRoundNumber?.Value
            };

            return res;
        }*/

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

//        public Tuple<Round, Round, StringValue> ExecuteTxsForExtraBlock()
//        {
//            var currentRoundInfo = SupplyPreviousRoundInfo();
//            var nextRoundInfo = GenerateNextRoundOrder();
//            var nextEBP = CalculateNextExtraBlockProducer();
//
//            return Tuple.Create(currentRoundInfo, nextRoundInfo, nextEBP);
//        }

        /// <summary>
        /// This method should return true if all the BPs restarted (and missed their time slots).
        /// </summary>
        /// <returns></returns>
        public bool CanRecoverDPoSInformation()
        {
            return false;
            /*try
            {
                //If DPoS information is already generated, return false;
                //Because this method doesn't responsible to initialize DPoS information.
                if (CurrentRoundNumber.Value == 0)
                {
                    return false;
                }

                var extraBlockTimeSlot = ExtraBlockTimeSlot.ToDateTime();
                var now = DateTime.UtcNow;
                if (now < extraBlockTimeSlot)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to check whether this node can recover DPoS mining.");
                return false;
            }

            return true;*/
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

        private string GetRoundInfoToString(UInt64Value roundNumber)
        {
            try
            {
                var result = "";

                var roundInfo = this[roundNumber];
                foreach (var minerInfo in roundInfo.RealTimeMinersInfo)
                {
                    result += GetAlias(minerInfo.Key) + ":\n";
                    result += "Is EBP:\t\t" + minerInfo.Value.IsExtraBlockProducer + "\n";
                    result += "Order:\t\t" + minerInfo.Value.Order + "\n";
                    result += "Mining Time:\t" + minerInfo.Value.ExpectedMiningTime.ToDateTime().ToLocalTime().ToString("u") + "\n";
                    result += "Signature:\t" + minerInfo.Value.Signature?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "Out Value:\t" + minerInfo.Value.OutValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "In Value:\t" + minerInfo.Value.InValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                }

                return result + $"\nEBP TimeSlot of current round: {roundInfo.GetEBPMiningTime().ToLocalTime():u}\n";
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

        private UInt64Value RoundNumberMinusOne(UInt64Value currentCount)
        {
            var current = currentCount.Value;
            current--;
            return new UInt64Value {Value = current};
        }

        /// <summary>
        /// Get local time
        /// </summary>
        /// <param name="offset">minutes</param>
        /// <returns></returns>
        private Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
        }

        private Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }

        /// <summary>
        /// In case of forgetting to check negative value.
        /// For now this method only used for generating order,
        /// so integer should be enough.
        /// </summary>
        /// <param name="uLongVal"></param>
        /// <param name="intVal"></param>
        /// <returns></returns>
        private int GetModulus(ulong uLongVal, int intVal)
        {
            return Math.Abs((int) (uLongVal % (ulong) intVal));
        }
    }
}