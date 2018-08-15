using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    public class AElfDPoSHelper
    {
        private readonly IDataProvider _dataProvider;
        private readonly BlockProducer _blockProducer;
        private readonly ILogger _logger;

        public BlockProducer BlockProducer
        {
            get
            {
                try
                {
                    return BlockProducer.Parser.ParseFrom(GetBytes(Globals.AElfDPoSBlockProducerString.CalculateHash()));
                }
                catch (Exception)
                {
                    return default(BlockProducer);
                }
            }
        }

        public UInt64Value CurrentRoundNumber
        {
            get
            {
                try
                {
                    return UInt64Value.Parser.ParseFrom(GetBytes(Globals.AElfDPoSCurrentRoundNumber.CalculateHash()));
                }
                catch (Exception)
                {
                    _logger.Info("Failed to get current round number.");
                    return new UInt64Value {Value = 0};
                }
            }
        }

        public Timestamp ExtraBlockTimeslot
        {
            get
            {
                try
                {
                    return Timestamp.Parser.ParseFrom(
                        GetBytes(Globals.AElfDPoSExtraBlockTimeslotString.CalculateHash()));
                }
                catch (Exception e)
                {
                    _logger.Error(e,
                        "The DPoS information has initialized but somehow the extra block timeslot is incorrect.");
                    return default(Timestamp);
                }
            }
        }

        public RoundInfo CurrentRoundInfo
        {
            get
            {
                try
                {
                    return RoundInfo.Parser.ParseFrom(GetBytes(CurrentRoundNumber.CalculateHash(),
                        Globals.AElfDPoSInformationString));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get DPoS information of current round.");
                    return default(RoundInfo);
                }
            }
        }

        public Int32Value MiningInterval
        {
            get
            {
                try
                {
                    return Int32Value.Parser.ParseFrom(GetBytes(Globals.AElfDPoSMiningIntervalString.CalculateHash()));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get DPoS mining interval.");
                    return new Int32Value {Value = Globals.AElfDPoSMiningInterval};
                }
            }
        }

        public StringValue FirstPlaceBlockProducerOfCurrentRound
        {
            get
            {
                try
                {
                    return StringValue.Parser.ParseFrom(GetBytes(CurrentRoundNumber.CalculateHash(),
                        Globals.AElfDPoSFirstPlaceOfEachRoundString));
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get first order prodocuer of current round.");
                    return default(StringValue);
                }
            }
        }

        /// <summary>
        /// Assert: Related value has surely exists in database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="resourceStr"></param>
        /// <returns></returns>
        private byte[] GetBytes(Hash keyHash, string resourceStr = "")
        {
            byte[] bytes;
            if (resourceStr != "")
            {
                bytes = _dataProvider.GetDataProvider(resourceStr).GetAsync(keyHash).Result;
            }
            else
            {
                bytes = _dataProvider.GetAsync(keyHash).Result;
            }

            return bytes;
        }

        public AElfDPoSHelper(IWorldStateDictator worldStateDictator, ECKeyPair keyPair, Hash chainId,
            BlockProducer blockProducer, Hash contractAddressHash, ILogger logger)
        {
            worldStateDictator.SetChainId(chainId);
            _blockProducer = blockProducer;
            _logger = logger;

            _dataProvider = worldStateDictator.GetAccountDataProvider(contractAddressHash).Result.GetDataProvider();
        }

        /// <summary>
        /// Get block producer information of current round.
        /// </summary>
        /// <param name="accountAddress"></param>
        public BPInfo this[string accountAddress]
        {
            get
            {
                try
                {
                    var bytes = GetBytes(CurrentRoundNumber.CalculateHash(), Globals.AElfDPoSInformationString);
                    var round = RoundInfo.Parser.ParseFrom(bytes);
                    return round.Info[accountAddress];
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to get BPInfo of current round.");
                    return default(BPInfo);
                }
            }
        }

        public RoundInfo this[UInt64Value roundNumber]
        {
            get
            {
                try
                {
                    var bytes = GetBytes(roundNumber.CalculateHash(), Globals.AElfDPoSInformationString);
                    var round = RoundInfo.Parser.ParseFrom(bytes);
                    return round;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to get RoundInfo of provided round number.");
                    return default(RoundInfo);
                }
            }
        }

        public async Task<bool> HasGenerated()
        {
            var bytes = await _dataProvider.GetAsync(Globals.AElfDPoSCurrentRoundNumber.CalculateHash());
            return bytes != null;
        }

        public DPoSInfo GenerateInfoForFirstTwoRounds()
        {
            var dict = new Dictionary<string, int>();

            // First round
            foreach (var node in _blockProducer.Nodes)
            {
                dict.Add(node, node[0]);
            }

            var sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;

            var enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound1 = new RoundInfo();

            var selected = _blockProducer.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};
                
                if (i == selected)
                {
                    bpInfo.IsEBP = true;

                }

                bpInfo.Order = i + 1;
                bpInfo.Signature = Hash.Generate();
                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * Globals.AElfDPoSMiningInterval + Globals.AElfWaitFirstRoundTime);

                infosOfRound1.Info.Add(enumerable[i], bpInfo);
            }

            // Second round
            dict = new Dictionary<string, int>();
            
            foreach (var node in _blockProducer.Nodes)
            {
                dict.Add(node, node[0]);
            }
            
            sortedMiningNodes =
                from obj in dict
                orderby obj.Value descending
                select obj.Key;
            
            enumerable = sortedMiningNodes.ToList();
            
            var infosOfRound2 = new RoundInfo();

            var addition = enumerable.Count * Globals.AElfDPoSMiningInterval + Globals.AElfDPoSMiningInterval;

            selected = _blockProducer.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                }

                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * Globals.AElfDPoSMiningInterval + addition + Globals.AElfWaitFirstRoundTime);
                bpInfo.Order = i + 1;

                infosOfRound2.Info.Add(enumerable[i], bpInfo);
            }

            var dPoSInfo = new DPoSInfo
            {
                RoundInfo = {infosOfRound1, infosOfRound2}
            };

            return dPoSInfo;
        }

        public async Task<RoundInfo> SupplyPreviousRoundInfo()
        {
            try
            {
                var roundInfo = CurrentRoundInfo;

                foreach (var info in roundInfo.Info)
                {
                    if (info.Value.InValue != null && info.Value.OutValue != null) continue;

                    var inValue = Hash.Generate();
                    var outValue = inValue.CalculateHash();

                    info.Value.OutValue = outValue;
                    info.Value.InValue = inValue;

                    //For the first round, the sig value is auto generated
                    if (info.Value.Signature == null && CurrentRoundNumber.Value != 1)
                    {
                        var signature = CalculateSignature(inValue);
                        info.Value.Signature = signature;
                    }

                    roundInfo.Info[info.Key] = info.Value;
                }

                return roundInfo;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to supply previous round info");
                return new RoundInfo();
            }
        }
        
        public Hash CalculateSignature(Hash inValue)
        {
            try
            {
                var add = Hash.Default;
                foreach (var node in _blockProducer.Nodes)
                {
                    var lastSignature = this[RoundNumberMinusOne(CurrentRoundNumber)].Info[node].Signature;
                    add = add.CalculateHashWith(lastSignature);
                }

                Hash sig = inValue.CalculateHashWith(add);
                return sig;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to calculate signature");
                return Hash.Default;
            }
        }

        public async Task<RoundInfo> GenerateNextRoundOrder()
        {
            try
            {
                var infosOfNextRound = new RoundInfo();
                var signatureDict = new Dictionary<Hash, string>();
                var orderDict = new Dictionary<int, string>();

                var blockProducerCount = _blockProducer.Nodes.Count;

                foreach (var node in _blockProducer.Nodes)
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

                var baseTimeslot = ExtraBlockTimeslot;

                //Maybe because something happened with setting extra block timeslot.
                if (baseTimeslot.ToDateTime().AddMilliseconds(Globals.AElfDPoSMiningInterval * 1.5) < GetTimestampOfUtcNow().ToDateTime())
                {
                    baseTimeslot = GetTimestampOfUtcNow();
                }

                for (var i = 0; i < orderDict.Count; i++)
                {
                    var bpInfoNew = new BPInfo
                    {
                        TimeSlot = GetTimestampWithOffset(baseTimeslot,
                            i * Globals.AElfDPoSMiningInterval + Globals.AElfDPoSMiningInterval * 2),
                        Order = i + 1
                    };

                    infosOfNextRound.Info[orderDict[i]] = bpInfoNew;
                }

                return infosOfNextRound;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to generate info of next round");
                return new RoundInfo();
            }
        }

        public StringValue CalculateNextExtraBlockProducer()
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
                var blockProducerCount = _blockProducer.Nodes.Count;
                var order = GetModulus(sigNum, blockProducerCount);

                var nextEBP = _blockProducer.Nodes[order];
            
                return new StringValue {Value = nextEBP.RemoveHexPrefix()};
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to set next extra block producer");
                return new StringValue {Value = ""};
            }
        }
        
        // ReSharper disable once UnusedMember.Global
        public async Task<StringValue> GetDPoSInfoToString()
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
                var roundInfoStr = await GetRoundInfoToString(new UInt64Value {Value = i});
                infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                i++;
            }

            var res = new StringValue
            {
                Value
                    = infoOfOneRound + $"EBP Timeslot of current round: {ExtraBlockTimeslot.ToDateTime().ToLocalTime():u}\n"
                             + "Current Round : " + CurrentRoundNumber?.Value
            };
            
            return res;
        }

        private async Task<string> GetDPoSInfoToStringOfLatestRounds(ulong countOfRounds)
        {
            try
            {
                if (CurrentRoundNumber.Value == 0)
                {
                    return "Somehow current round number is 0.";
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

                    var roundInfoStr = await GetRoundInfoToString(new UInt64Value {Value = i});
                    infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                    i++;
                }

                return infoOfOneRound + $"EBP Timeslot of current round: {ExtraBlockTimeslot.ToDateTime().ToLocalTime():u}\n"
                                      + $"Current Round : {CurrentRoundNumber.Value}";
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to get dpos info");
                return "";
            }
        }
        
        public async Task<Tuple<RoundInfo, RoundInfo, StringValue>> ExecuteTxsForExtraBlock()
        {
            var currentRoundInfo = await SupplyPreviousRoundInfo();
            var nextRoundInfo = await GenerateNextRoundOrder();
            var nextEBP = CalculateNextExtraBlockProducer();
            
            return Tuple.Create(currentRoundInfo, nextRoundInfo, nextEBP);
        }
        
        /// <summary>
        /// This method should return true if all the BPs restarted (and missed their timeslots).
        /// </summary>
        /// <returns></returns>
        public bool CanRecoverDPoSInformation()
        {
            try
            {
                //If DPoS information is already generated, return false;
                //Because this method doesn't resposible to initialize DPoS information.
                if (CurrentRoundNumber.Value == 0)
                {
                    return false;
                }

                var extraBlockTimeslot = ExtraBlockTimeslot.ToDateTime();
                var now = DateTime.UtcNow;
                if (now < extraBlockTimeslot)
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
            Globals.AElfDPoSMiningInterval = MiningInterval.Value;
        }
        
        public async Task<string> GetDPoSInfo(ulong height)
        {
            _logger?.Trace("Log dpos information - Start");
            return await GetDPoSInfoToStringOfLatestRounds(Globals.AElfDPoSLogRoundCount) + $". Current height: {height}";
        }

        private async Task<string> GetRoundInfoToString(UInt64Value roundNumber)
        {
            try
            {
                var result = "";

                foreach (var bpInfo in this[roundNumber].Info)
                {
                    result += bpInfo.Key + ":\n";
                    result += "IsEBP:\t\t" + bpInfo.Value.IsEBP + "\n";
                    result += "Order:\t\t" + bpInfo.Value.Order + "\n";
                    result += "Timeslot:\t" + bpInfo.Value.TimeSlot.ToDateTime().ToLocalTime().ToString("u") + "\n";
                    result += "Signature:\t" + bpInfo.Value.Signature?.Value.ToByteArray().ToHex().Remove(0, 2) + "\n";
                    result += "Out Value:\t" + bpInfo.Value.OutValue?.Value.ToByteArray().ToHex().Remove(0, 2) + "\n";
                    result += "In Value:\t" + bpInfo.Value.InValue?.Value.ToByteArray().ToHex().Remove(0, 2) + "\n";
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
    }
}