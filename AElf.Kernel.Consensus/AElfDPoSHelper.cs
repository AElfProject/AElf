using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Extensions;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    // TODO: Cache
    // ReSharper disable InconsistentNaming
    public class AElfDPoSHelper
    {
        private readonly Hash _chainId;
        private readonly Miners _miners;
        private readonly ILogger _logger;
        private readonly Address _contractAddressHash;
        private readonly IStateStore _stateStore;

        public AElfDPoSInformation DpoSInformation { get; private set; }

        private DataProvider DataProvider
        {
            get
            {
                var dp = DataProvider.GetRootDataProvider(_chainId, _contractAddressHash);
                dp.StateStore = _stateStore;
                return dp;
            }
        }

        public Miners Miners
        {
            get
            {
                try
                {
                    var miners =
                        Miners.Parser.ParseFrom(
                            GetBytes<Miners>(Hash.FromString(GlobalConfig.AElfDPoSBlockProducerString)));
                    return miners;
                }
                catch (Exception)
                {
                    return new Miners();
                }
            }
        }

        public UInt64Value CurrentRoundNumber
        {
            get
            {
                try
                {
                    _logger?.Trace("Getting Current Round Number.");
                    var number = UInt64Value.Parser.ParseFrom(
                        GetBytes<UInt64Value>(Hash.FromString(GlobalConfig.AElfDPoSCurrentRoundNumber)));
                    _logger?.Trace("Current Round Number: " + number.Value);
                    return number;
                }
                catch (Exception)
                {
                    return new UInt64Value {Value = 0};
                }
            }
        }

        public Timestamp ExtraBlockTimeSlot
        {
            get
            {
                try
                {
                    return Timestamp.Parser.ParseFrom(
                        GetBytes<Timestamp>(Hash.FromString(GlobalConfig.AElfDPoSExtraBlockTimeSlotString)));
                }
                catch (Exception e)
                {
                    _logger.Error(e,
                        "The DPoS information has initialized but somehow the extra block timeslot is incorrect.\n");
                    return default(Timestamp);
                }
            }
        }

        private Round CurrentRoundInfo
        {
            get
            {
                try
                {
                    return Round.Parser.ParseFrom(GetBytes<Round>(Hash.FromMessage(CurrentRoundNumber),
                        GlobalConfig.AElfDPoSInformationString));
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
                        GetBytes<SInt32Value>(Hash.FromString(GlobalConfig.AElfDPoSMiningIntervalString)));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get DPoS mining interval.\n");
                    return new SInt32Value {Value = GlobalConfig.AElfDPoSMiningInterval};
                }
            }
        }

        private StringValue FirstPlaceBlockProducerOfCurrentRound
        {
            get
            {
                try
                {
                    return StringValue.Parser.ParseFrom(GetBytes<StringValue>(Hash.FromMessage(CurrentRoundNumber),
                        GlobalConfig.AElfDPoSFirstPlaceOfEachRoundString));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get first order prodocuer of current round.");
                    return new StringValue {Value = ""};
                }
            }
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        private byte[] GetBytes<T>(Hash keyHash, string resourceStr = "") where T : IMessage, new()
        {
            return resourceStr != ""
                ? DataProvider.GetChild(resourceStr).GetAsync<T>(keyHash).Result
                : DataProvider.GetAsync<T>(keyHash).Result;
        }

        public AElfDPoSHelper(Hash chainId, Miners miners, Address contractAddressHash, IStateStore stateStore)
        {
            _chainId = chainId;
            _miners = miners;
            _contractAddressHash = contractAddressHash;
            _stateStore = stateStore;

            _logger = LogManager.GetLogger(nameof(AElfDPoSHelper));
        }

        /// <summary>
        /// Get block producer information of current round.
        /// </summary>
        /// <param name="accountAddress"></param>
        public BlockProducer this[string accountAddress]
        {
            get
            {
                try
                {
                    var bytes = GetBytes<Round>(Hash.FromMessage(CurrentRoundNumber),
                        GlobalConfig.AElfDPoSInformationString);
                    var round = Round.Parser.ParseFrom(bytes);
                    return round.BlockProducers[accountAddress];
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to get Block Producer information of current round.");
                    return default(BlockProducer);
                }
            }
        }

        private Round this[UInt64Value roundNumber]
        {
            get
            {
                try
                {
                    var bytes = GetBytes<Round>(Hash.FromMessage(roundNumber), GlobalConfig.AElfDPoSInformationString);
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

        public async Task<bool> HasGenerated()
        {
            var bytes = await DataProvider.GetAsync<Miners>(Hash.FromString(GlobalConfig.AElfDPoSBlockProducerString));
            return bytes != null && bytes.Length > 0;
        }

        public AElfDPoSInformation GenerateInfoForFirstTwoRounds()
        {
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var node in _miners.Nodes)
            {
                dict.Add(node, node[0]);
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();

            var infosOfRound1 = new Round();

            var selected = _miners.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BlockProducer {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                }

                bpInfo.Order = i + 1;
                bpInfo.Signature = Hash.Generate();
                bpInfo.TimeSlot =
                    GetTimestampOfUtcNow(i * GlobalConfig.AElfDPoSMiningInterval + GlobalConfig.AElfWaitFirstRoundTime);

                infosOfRound1.BlockProducers.Add(enumerable[i], bpInfo);
            }

            // Second round
            dict = new Dictionary<string, int>();

            foreach (var node in _miners.Nodes)
            {
                dict.Add(node, node[0]);
            }

            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            enumerable = sortedMiningNodes.ToList();

            var infosOfRound2 = new Round();

            var addition = enumerable.Count * GlobalConfig.AElfDPoSMiningInterval + GlobalConfig.AElfDPoSMiningInterval;

            selected = _miners.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BlockProducer {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                }

                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * GlobalConfig.AElfDPoSMiningInterval + addition +
                                                       GlobalConfig.AElfWaitFirstRoundTime);
                bpInfo.Order = i + 1;

                infosOfRound2.BlockProducers.Add(enumerable[i], bpInfo);
            }

            infosOfRound1.RoundNumber = 1;
            infosOfRound2.RoundNumber = 2;

            var dPoSInfo = new AElfDPoSInformation
            {
                Rounds = {infosOfRound1, infosOfRound2}
            };

            return dPoSInfo;
        }

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
                foreach (var node in _miners.Nodes)
                {
                    var lastSignature = this[RoundNumberMinusOne(CurrentRoundNumber)].BlockProducers[node].Signature;
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

                var blockProducerCount = _miners.Nodes.Count;

                foreach (var node in _miners.Nodes)
                {
                    var s = this[node].Signature;
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

                var blockTimeSlot = ExtraBlockTimeSlot;

                //Maybe because something happened with setting extra block time slot.
                if (blockTimeSlot.ToDateTime().AddMilliseconds(GlobalConfig.AElfDPoSMiningInterval * 1.5) <
                    GetTimestampOfUtcNow().ToDateTime())
                {
                    blockTimeSlot = GetTimestampOfUtcNow();
                }

                for (var i = 0; i < orderDict.Count; i++)
                {
                    var bpInfoNew = new BlockProducer
                    {
                        TimeSlot = GetTimestampWithOffset(blockTimeSlot,
                            i * GlobalConfig.AElfDPoSMiningInterval + GlobalConfig.AElfDPoSMiningInterval * 2),
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
                var blockProducerCount = _miners.Nodes.Count;
                var order = GetModulus(sigNum, blockProducerCount);

                var nextEBP = _miners.Nodes[order];

                return new StringValue {Value = nextEBP.RemoveHexPrefix()};
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to set next extra block producer");
                return new StringValue {Value = ""};
            }
        }

        // ReSharper disable once UnusedMember.Global
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
                    infoOfOneRound + $"EBP TimeSlot of current round: {ExtraBlockTimeSlot.ToDateTime().ToLocalTime():u}\n"
                                   + $"Current Round : {CurrentRoundNumber.Value}";
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to get dpos info");
                return "";
            }
        }

        public Tuple<Round, Round, StringValue> ExecuteTxsForExtraBlock()
        {
            var currentRoundInfo = SupplyPreviousRoundInfo();
            var nextRoundInfo = GenerateNextRoundOrder();
            var nextEBP = CalculateNextExtraBlockProducer();

            return Tuple.Create(currentRoundInfo, nextRoundInfo, nextEBP);
        }

        /// <summary>
        /// This method should return true if all the BPs restarted (and missed their time slots).
        /// </summary>
        /// <returns></returns>
        public bool CanRecoverDPoSInformation()
        {
            try
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

            return true;
        }

        public void SyncMiningInterval()
        {
            GlobalConfig.AElfDPoSMiningInterval = MiningInterval.Value;
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
            _logger?.Trace("Getting Current Round Information.");
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

                foreach (var bpInfo in this[roundNumber].BlockProducers)
                {
                    result += bpInfo.Key + ":\n";
                    result += "IsEBP:\t\t" + bpInfo.Value.IsEBP + "\n";
                    result += "Order:\t\t" + bpInfo.Value.Order + "\n";
                    result += "Time Slot:\t" + bpInfo.Value.TimeSlot.ToDateTime().ToLocalTime().ToString("u") + "\n";
                    result += "Signature:\t" + bpInfo.Value.Signature?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "Out Value:\t" + bpInfo.Value.OutValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                    result += "In Value:\t" + bpInfo.Value.InValue?.Value.ToByteArray().ToHex().RemoveHexPrefix() +
                              "\n";
                }

                return result + "\n";
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Failed to get dpos info of round {roundNumber.Value}");
                return "";
            }
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
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
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Timestamp GetTimestampOfUtcNow(int offset = 0)
        {
            return Timestamp.FromDateTime(DateTime.UtcNow.AddMilliseconds(offset));
        }

        private Timestamp GetTimestampWithOffset(Timestamp origin, int offset)
        {
            return Timestamp.FromDateTime(origin.ToDateTime().AddMilliseconds(offset));
        }

        /// <summary>
        /// In case of forgetting to check negativee value.
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
    }
}