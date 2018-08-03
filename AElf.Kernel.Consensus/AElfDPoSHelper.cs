using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    public class AElfDPoSHelper
    {
        private readonly ECKeyPair _keyPair;
        private readonly IDataProvider _dataProvider;
        private readonly BlockProducer _blockProducer;
        private readonly IChainManager _chainManager;
        private readonly ILogger _logger;
        private readonly Hash _chainId;

        public BlockProducer BlockProducer
        {
            get
            {
                try
                {
                    return BlockProducer.Parser.ParseFrom(_dataProvider
                        .GetAsync(Globals.AElfDPoSBlockProducerString.CalculateHash()).Result);
                }
                catch (Exception)
                {
                    _logger.Info("The DPoS mining nodes list is empty if you see this log." +
                                  "Maybe you should re-check the config file or election part.");
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
                    var bytes = _dataProvider.GetAsync(Globals.AElfDPoSCurrentRoundNumber.CalculateHash()).Result;
                    return UInt64Value.Parser.ParseFrom(bytes);
                }
                catch (Exception)
                {
                    _logger.Info("The DPoS information hasn't initialized yet if you see this log.");
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
                    return Timestamp.Parser.ParseFrom(_dataProvider
                        .GetAsync(Globals.AElfDPoSExtraBlockTimeslotString.CalculateHash())
                        .Result);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "The DPoS information has initialized but somehow the extra block timeslot is incorrect.");
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
                    return RoundInfo.Parser.ParseFrom(_dataProvider.GetDataProvider(Globals.AElfDPoSInformationString)
                        .GetAsync(CurrentRoundNumber.CalculateHash()).Result);
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get DPoS information of current round.");
                    return default(RoundInfo);
                }
            }
        }

        public StringValue FirstPlaceBlockProducerOfCurrentRound
        {
            get
            {
                try
                {
                    return StringValue.Parser.ParseFrom(_dataProvider
                        .GetDataProvider(Globals.AElfDPoSFirstPlaceOfEachRoundString)
                        .GetAsync(CurrentRoundNumber.CalculateHash()).Result);
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Failed to get first order prodocuer of current round.");
                    return default(StringValue);
                }
            }
        }
        
        public AElfDPoSHelper(IWorldStateDictator worldStateDictator, ECKeyPair keyPair, Hash chainId,
            BlockProducer blockProducer, Hash contractAddressHash, IChainManager chainManager, ILogger logger)
        {
            worldStateDictator.SetChainId(chainId);
            _keyPair = keyPair;
            _blockProducer = blockProducer;
            _logger = logger;
            _chainManager = chainManager;
            _chainId = chainId;

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
                    return RoundInfo.Parser.ParseFrom(_dataProvider.GetDataProvider(Globals.AElfDPoSInformationString)
                        .GetAsync(CurrentRoundNumber.CalculateHash()).Result).Info[accountAddress];
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to get BPInfo of current round.");
                    return default(BPInfo);
                }
            }
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
                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * Globals.AElfMiningTime + Globals.AElfWaitFirstRoundTime);

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

            var addition = enumerable.Count * Globals.AElfMiningTime + Globals.AElfMiningTime;

            selected = _blockProducer.Nodes.Count / 2;
            for (var i = 0; i < enumerable.Count; i++)
            {
                var bpInfo = new BPInfo {IsEBP = false};

                if (i == selected)
                {
                    bpInfo.IsEBP = true;
                }

                bpInfo.TimeSlot = GetTimestampOfUtcNow(i * Globals.AElfMiningTime + addition + Globals.AElfWaitFirstRoundTime);
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
                        var signature = await CalculateSignature(inValue);
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
        
        public async Task<Hash> CalculateSignature(Hash inValue)
        {
            try
            {
                var add = Hash.Default;
                foreach (var node in _blockProducer.Nodes)
                {
                    var bpInfo = await GetBlockProducerInfoOfSpecificRound(node, RoundNumberMinusOne(CurrentRoundNumber));
                    var lastSignature = bpInfo.Signature;
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
                    var s = (await GetBlockProducerInfoOfCurrentRound(node)).Signature;
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
                if (baseTimeslot.ToDateTime().AddMilliseconds(Globals.AElfMiningTime * 1.5) < GetTimestampOfUtcNow().ToDateTime())
                {
                    baseTimeslot = GetTimestampOfUtcNow();
                }

                for (var i = 0; i < orderDict.Count; i++)
                {
                    var bpInfoNew = new BPInfo
                    {
                        TimeSlot = GetTimestampWithOffset(baseTimeslot,
                            i * Globals.AElfMiningTime + Globals.AElfMiningTime * 2),
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

        public async Task<StringValue> CalculateNextExtraBlockProducer()
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

                // ReSharper disable once InconsistentNaming
                var nextEBP = _blockProducer.Nodes[order];
            
                return new StringValue {Value = nextEBP.RemoveHexPrefix()};
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to set next extra block producer");
                return new StringValue {Value = ""};
            }
        }
        
        // ReSharper disable once InconsistentNaming
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

            // ReSharper disable once InconsistentNaming
            var eBPTimeslot = Timestamp.Parser.ParseFrom(await _dataProvider.GetAsync(Globals.AElfDPoSExtraBlockTimeslotString.CalculateHash()));

            var res = new StringValue
            {
                Value
                    = infoOfOneRound + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
                             + "Current Round : " + CurrentRoundNumber?.Value
            };
            
            return res;
        }

        // ReSharper disable once InconsistentNaming
        private async Task<string> GetDPoSInfoToStringOfLatestRounds(ulong countOfRounds)
        {
            try
            {
                if (CurrentRoundNumber.Value == 0)
                {
                    return "No DPoS Information, maybe failed to generate related information or synchronize blocks.\n" +
                           "Please check the account you config has the right to produce block if this node is the first" +
                           "one to start.";
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
            
                // ReSharper disable once InconsistentNaming
                var eBPTimeslot = Timestamp.Parser.ParseFrom(await _dataProvider.GetAsync(Globals.AElfDPoSExtraBlockTimeslotString.CalculateHash()));

                return infoOfOneRound + $"EBP Timeslot of current round: {eBPTimeslot.ToDateTime().ToLocalTime():u}\n"
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
            // ReSharper disable once InconsistentNaming
            var nextEBP = await CalculateNextExtraBlockProducer();
            
            return Tuple.Create(currentRoundInfo, nextRoundInfo, nextEBP);
        }
        
        // ReSharper disable once InconsistentNaming
        public void StartConsensusLog(bool doLogsAboutConsensus = true)
        {
            new EventLoopScheduler().Schedule(() =>
            {
                var dPoSInfo = "";

                var intervalSequnce = GetIntervalObservable();
                intervalSequnce.Subscribe
                (
                    async x =>
                    {
                        var currentHeightOfThisNode = (long) await _chainManager.GetChainCurrentHeight(_chainId);

                        if (!doLogsAboutConsensus) 
                            return;
                        
                        // ReSharper disable once InconsistentNaming
                        var currentDPoSInfo = await GetDPoSInfo(currentHeightOfThisNode);
                        if (dPoSInfo == currentDPoSInfo)
                            return;
                            
                        dPoSInfo = currentDPoSInfo;
                        _logger?.Log(LogLevel.Debug, dPoSInfo);

                    }
                );
            });
        }
        
        // ReSharper disable once InconsistentNaming
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
        
        // ReSharper disable once InconsistentNaming
        private async Task<string> GetDPoSInfo(long height)
        {
            return await GetDPoSInfoToStringOfLatestRounds(Globals.AElfDPoSLogRoundCount) + $". Current height: {height}";
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private IObservable<long> GetIntervalObservable()
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(Globals.AElfCheckTime));
        }

        private async Task<string> GetRoundInfoToString(UInt64Value roundNumber)
        {
            try
            {
                var bytes = await _dataProvider.GetDataProvider(Globals.AElfDPoSInformationString)
                    .GetAsync(roundNumber.CalculateHash());
                var info = RoundInfo.Parser.ParseFrom(bytes);
                
                var result = "";

                foreach (var bpInfo in info.Info)
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
        
        private async Task<Timestamp> GetTimeSlot(string accountAddress)
        {
            return (await GetBlockProducerInfoOfCurrentRound(accountAddress)).TimeSlot;
        }

        private async Task<BPInfo> GetBlockProducerInfoOfCurrentRound(string accountAddress)
        {
            var bytes = await _dataProvider.GetDataProvider(Globals.AElfDPoSInformationString).GetAsync(CurrentRoundNumber.CalculateHash());
            var roundInfo = RoundInfo.Parser.ParseFrom(bytes);
            return roundInfo.Info[accountAddress];
        }
        
        private async Task<BPInfo> GetBlockProducerInfoOfSpecificRound(string accountAddress, UInt64Value roundNumber)
        {
            var bytes = await _dataProvider.GetDataProvider(Globals.AElfDPoSInformationString).GetAsync(roundNumber.CalculateHash());
            var roundInfo = RoundInfo.Parser.ParseFrom(bytes);
            return roundInfo.Info[accountAddress];
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private string AddressHashToString(Hash accountHash)
        {
            return accountHash.ToAccount().ToHex().Remove(0, 2);
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