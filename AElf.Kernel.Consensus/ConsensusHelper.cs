using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel.Managers;
using AElf.Configuration.Config.Consensus;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable MemberCanBeMadeStatic.Local
    // ReSharper disable UnusedMember.Global
    public class ConsensusHelper
    {

        public ConsensusHelper()
        {
            Logger=NullLogger<ConsensusHelper>.Instance;
        }
        private readonly IMinersManager _minersManager;
        private readonly ConsensusDataReader _reader;        
        public ILogger<ConsensusHelper> Logger { get; set; }

        public UInt64Value GetCurrentRoundNumber(int chainId)
        {
            try
            {
                return UInt64Value.Parser.ParseFrom(
                    _reader.ReadFiled<UInt64Value>(chainId, GlobalConfig.AElfDPoSCurrentRoundNumber));
            }
            catch (Exception)
            {
                return new UInt64Value {Value = 0};
            }
        }

        public UInt64Value GetCurrentTermNumber(int chainId)
        {
            try
            {
                return UInt64Value.Parser.ParseFrom(
                    _reader.ReadFiled<UInt64Value>(chainId, GlobalConfig.AElfDPoSCurrentTermNumber));
            }
            catch (Exception)
            {
                return new UInt64Value {Value = 0};
            }
        }

        public UInt64Value GetBlockchainAge(int chainId)
        {
            try
            {
                return UInt64Value.Parser.ParseFrom(
                    _reader.ReadFiled<UInt64Value>(chainId, GlobalConfig.AElfDPoSAgeFieldString));
            }
            catch (Exception)
            {
                return new UInt64Value {Value = 0};
            }
        }

        public Timestamp GetBlockchainStartTimestamp(int chainId)
        {
            try
            {
                return Timestamp.Parser.ParseFrom(
                    _reader.ReadFiled<Timestamp>(chainId, GlobalConfig.AElfDPoSBlockchainStartTimestamp));
            }
            catch (Exception)
            {
                return DateTime.UtcNow.ToTimestamp();
            }
        }

        public Candidates GetCandidates(int chainId)
        {
            try
            {
                var candidates = Candidates.Parser.ParseFrom(
                    _reader.ReadFiled<Candidates>(chainId, GlobalConfig.AElfDPoSCandidatesString));
                if (candidates.PublicKeys.Count < GlobalConfig.BlockProducerNumber)
                {
                    throw new Exception();
                }

                return candidates;
            }
            catch (Exception)
            {
                Logger.LogTrace("No candidate, so the miners of next term will still be the initial miners.");
                var initialMiners = _minersManager.GetMiners(0).Result.PublicKeys.ToCandidates();
                initialMiners.IsInitialMiners = true;
                return initialMiners;
            }
        }

        private SInt32Value GetMiningInterval(int chainId)
        {
            try
            {
                return SInt32Value.Parser.ParseFrom(
                    _reader.ReadFiled<SInt32Value>(chainId, GlobalConfig.AElfDPoSMiningIntervalString));
            }
            catch (Exception)
            {
                return new SInt32Value {Value = ConsensusConfig.Instance.DPoSMiningInterval};
            }
        }

        public ConsensusHelper(IMinersManager minersManager, ConsensusDataReader reader):this()
        {
            _minersManager = minersManager;
            _reader = reader;
        }

        private Round GetRound(int chainId, UInt64Value roundNumber)
        {
            try
            {
                var bytes = _reader.ReadMap<Round>(chainId, roundNumber, GlobalConfig.AElfDPoSRoundsMapString);
                var round = Round.Parser.ParseFrom(bytes);
                return round;
            }
            catch (Exception ex)
            {
                Logger.LogTrace(ex, $"Error while getting Round information of round {roundNumber.Value}.");
                return default(Round);
            }
        }

        public bool TryToGetVictories(int chainId, out List<string> victories)
        {
            var ticketsMap = new Dictionary<string, ulong>();
            victories = new List<string>();
            var candidates = GetCandidates(chainId);
            if (candidates.PublicKeys.Count < GlobalConfig.BlockProducerNumber)
            {
                return false;
            }

            foreach (var candidate in candidates.PublicKeys)
            {
                var tickets = GetTickets(chainId, candidate);
                if (tickets.ObtainedTickets != 0)
                {
                    ticketsMap[candidate] = tickets.ObtainedTickets;
                }
            }

            if (ticketsMap.Keys.Count < GlobalConfig.BlockProducerNumber)
            {
                return false;
            }

            victories = ticketsMap.OrderByDescending(tm => tm.Value).Take(GlobalConfig.BlockProducerNumber)
                .Select(tm => tm.Key)
                .ToList();
            return !candidates.IsInitialMiners;
        }

        private Tickets GetTickets(int chainId, string candidatePublicKey)
        {
            var bytes = _reader.ReadMap<Tickets>(chainId, candidatePublicKey.ToStringValue(),
                GlobalConfig.AElfDPoSTicketsMapString);
            return bytes == null ? new Tickets() : Tickets.Parser.ParseFrom(bytes);
        }

        private string GetDPoSInfoToStringOfLatestRounds(int chainId, ulong countOfRounds)
        {
            try
            {
                if (GetCurrentRoundNumber(chainId).Value == 0)
                {
                    return "Somehow current round number is 0";
                }

                if (countOfRounds == 0)
                {
                    return "";
                }

                var currentRoundNumber = GetCurrentRoundNumber(chainId).Value;
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

                    var roundInfoStr = GetRoundInfoToString(chainId, new UInt64Value {Value = i});
                    infoOfOneRound += $"\n[Round {i}]\n" + roundInfoStr;
                    i++;
                }

                return infoOfOneRound + $"Current round: {GetCurrentRoundNumber(chainId).Value}";
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to get dpos info");
                return "";
            }
        }

        /// <summary>
        /// This method should return true if all the BPs restarted (and missed their time slots).
        /// </summary>
        /// <returns></returns>
        public bool CanRecoverDPoSInformation()
        {
            return GlobalConfig.BlockProducerNumber == 1;
        }

        public ulong CalculateBlockchainAge(int chainId)
        {
            return (ulong) (DateTime.UtcNow - GetBlockchainStartTimestamp(chainId).ToDateTime()).TotalMinutes + 1;
        }

        public void SyncMiningInterval(int chainId)
        {
            ConsensusConfig.Instance.DPoSMiningInterval = GetMiningInterval(chainId).Value;
            Logger.LogInformation($"Set AElf DPoS mining interval to: {ConsensusConfig.Instance.DPoSMiningInterval} ms.");
        }

        public void LogDPoSInformation(int chainId, ulong height)
        {
            Logger.LogTrace("Log dpos information - Start");
            Logger.LogTrace(GetDPoSInfoToStringOfLatestRounds(chainId, GlobalConfig.AElfDPoSLogRoundCount) +$". Current height: {height}. Current term: {GetCurrentTermNumber(chainId).Value}. Currentage:{GetBlockchainAge(chainId).Value}");
            Logger.LogTrace(GetCurrentElectionInformation(chainId));
            Logger.LogTrace("Log dpos information - End");
        }

        /// <summary>
        /// Valid candidate means someone has voted him.
        /// </summary>
        /// <param name="validCandidates"></param>
        /// <returns></returns>
        private bool TryToGetValidCandidates(int chainId, out List<string> validCandidates)
        {
            validCandidates = new List<string>();
            foreach (var candidate in GetCandidates(chainId).PublicKeys)
            {
                if (GetTickets(chainId, candidate).ObtainedTickets > 0)
                {
                    validCandidates.Add(candidate);
                }
            }

            return validCandidates.Any();
        }

        private string GetCurrentElectionInformation(int chainId)
        {
            var result = "";
            var dictionary = new Dictionary<string, ulong>();
            if (!TryToGetValidCandidates(chainId, out var candidates))
                return result;

            foreach (var candidatePublicKey in candidates)
            {
                var tickets = GetTickets(chainId, candidatePublicKey);

                dictionary.Add(GetAlias(chainId, candidatePublicKey), tickets.ObtainedTickets);
            }

            result += "\nElection information:\n";

            return dictionary.OrderByDescending(kv => kv.Value)
                .Aggregate(result, (current, pair) => current + $"[{pair.Key}]\n{pair.Value}\n");
        }

        public Round GetCurrentRoundInfo(int chainId)
        {
            return GetCurrentRoundNumber(chainId).Value != 0 ? GetRound(chainId, GetCurrentRoundNumber(chainId)) : null;
        }

        public Miners GetCurrentMiners(int chainId)
        {
            Logger.LogTrace($"Current term number: {GetCurrentTermNumber(chainId).Value}");
            var bytes = _reader.ReadMap<Miners>(chainId, GetCurrentTermNumber(chainId), GlobalConfig.AElfDPoSMinersMapString);
            var miners = AElf.Kernel.Miners.Parser.ParseFrom(bytes);
            return miners;
        }

        public TermSnapshot GetLatestTermSnapshot(int chainId)
        {
            var bytes = _reader.ReadMap<TermSnapshot>(chainId, GetCurrentTermNumber(chainId), GlobalConfig.AElfDPoSSnapshotMapString);
            var snapshot = TermSnapshot.Parser.ParseFrom(bytes);
            return snapshot;
        }

        public bool TryGetRoundInfo(int chainId, ulong roundNumber, out Round roundInfo)
        {
            if (roundNumber == 0)
            {
                roundInfo = null;
                return false;
            }

            var info = GetRound(chainId, roundNumber.ToUInt64Value());
            if (info != null)
            {
                roundInfo = info;
                return true;
            }

            roundInfo = null;
            return false;
        }

        private string GetRoundInfoToString(int chainId, UInt64Value roundNumber)
        {
            try
            {
                var result = "";

                var roundInfo = GetRound(chainId, roundNumber);
                foreach (var minerInfo in roundInfo.RealTimeMinersInfo.OrderBy(m => m.Value.Order))
                {
                    result += GetAlias(chainId, minerInfo.Key) +
                              (minerInfo.Value.IsExtraBlockProducer ? " [Current EBP]:\n" : ":\n");
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
                    // TODO: `IsForked` Not implemented yet, maybe useless.
                    result += "Is Forked:\t" + minerInfo.Value.IsForked + "\n";
                    result += "Missed Slots:\t" + minerInfo.Value.MissedTimeSlots + "\n";
                    result += "Latest Missed:\t" + minerInfo.Value.LatestMissedTimeSlots + "\n";
                }

                return result +
                       $"\nEBP TimeSlot of current round: {roundInfo.GetEBPMiningTime(GetMiningInterval(chainId).Value).ToLocalTime():u}\n";
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to get dpos info of round {roundNumber.Value}");
                return "";
            }
        }

        private string GetAlias(int chainId, string publicKey)
        {
            var bytes = _reader.ReadMap<StringValue>(chainId, new StringValue {Value = publicKey},
                GlobalConfig.AElfDPoSAliasesMapString);
            return bytes == null
                ? publicKey.Substring(0, GlobalConfig.AliasLimit)
                : StringValue.Parser.ParseFrom(bytes).Value;
        }
    }
}