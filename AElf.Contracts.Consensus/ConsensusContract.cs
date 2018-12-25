using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using AElf.Contracts.Consensus.Contracts;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public class ConsensusContract : CSharpSmartContract
    {
        private DataCollection Collection => new DataCollection
        {
            CurrentRoundNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentRoundNumber),
            MiningIntervalField = new Int32Field(GlobalConfig.AElfDPoSMiningIntervalString),
            CandidatesField = new PbField<Candidates>(GlobalConfig.AElfDPoSCandidatesString),
            TermNumberLookupField = new PbField<TermNumberLookUp>(GlobalConfig.AElfDPoSTermNumberLookupString),
            AgeField = new UInt64Field(GlobalConfig.AElfDPoSAgeFieldString),
            CurrentTermNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentTermNumber),
            BlockchainStartTimestamp = new PbField<Timestamp>(GlobalConfig.AElfDPoSBlockchainStartTimestamp),
            VotesCountField = new UInt64Field(GlobalConfig.AElfVotesCountString),
            TicketsCountField = new UInt64Field(GlobalConfig.AElfTicketsCountString),

            RoundsMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSRoundsMapString),
            MinersMap = new Map<UInt64Value, Miners>(GlobalConfig.AElfDPoSMinersMapString),
            TicketsMap = new Map<StringValue, Tickets>(GlobalConfig.AElfDPoSTicketsMapString),
            SnapshotField = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotMapString),
            AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString),
            AliasesLookupMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesLookupMapString),
            HistoryMap = new Map<StringValue, CandidateInHistory>(GlobalConfig.AElfDPoSHistoryMapString),
            AgeToRoundNumberMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSAgeToRoundNumberMapString)
        };

        private Process Process => new Process(Collection);

        private Election Election => new Election(Collection);
        
        private Validation Validation => new Validation(Collection);

        #region Process
        
        [View]
        public Round GetRoundInfo(ulong roundNumber)
        {
            Api.Assert(Collection.RoundsMap.TryGet(roundNumber.ToUInt64Value(), out var roundInfo), GlobalConfig.RoundNumberNotFound);
            return roundInfo;
        }

        [View]
        public ulong GetCurrentRoundNumber(string empty)
        {
            return Collection.CurrentRoundNumberField.GetValue();
        }
        
        public void InitialTerm(Term term, int logLevel)
        {
            Api.Assert(term.FirstRound.RoundNumber == 1);
            Api.Assert(term.SecondRound.RoundNumber == 2);
            
            Process.InitialTerm(term, logLevel);
        }
        
        public void NextTerm(Term term)
        {
            Process.NextTerm(term);
        }

        public void NextRound(Forwarding forwarding)
        {
            Process.NextRound(forwarding);
        }

        public void PackageOutValue(ToPackage toPackage)
        {
            Process.PublishOutValue(toPackage);
        }

        public void BroadcastInValue(ToBroadcast toBroadcast)
        {
            Process.PublishInValue(toBroadcast);
        }
        
        #endregion Process

        #region Election
        
        [View]
        public ulong GetCurrentTermNumber(string empty)
        {
            return Collection.CurrentTermNumberField.GetValue();
        }

        [View]
        public bool IsCandidate(string publicKey)
        {
            return Collection.CandidatesField.GetValue().PublicKeys.Contains(publicKey);
        }
        
        [View]
        public StringList GetCandidatesList(string empty)
        {
            return Collection.CandidatesField.GetValue().PublicKeys.ToList().ToStringList();
        }
        
        [View]
        public string GetCandidatesListToFriendlyString(string empty)
        {
            return GetCandidatesList(empty).ToString();
        }

        [View]
        public CandidateInHistory GetCandidateHistoryInfo(string publicKey)
        {
            Api.Assert(Collection.HistoryMap.TryGet(publicKey.ToStringValue(), out var info),
                GlobalConfig.CandidateNotFound);
            return info;
        }
        
        [View]
        public string GetCandidateHistoryInfoToFriendlyString(string publicKey)
        {
            return GetCandidateHistoryInfo(publicKey).ToString();
        }

        [View]
        public Miners GetCurrentMiners(string empty)
        {
            var currentTermNumber = Collection.CurrentTermNumberField.GetValue();
            if (currentTermNumber == 0)
            {
                currentTermNumber = 1;
            }
            Api.Assert(Collection.MinersMap.TryGet(currentTermNumber.ToUInt64Value(), out var currentMiners),
                GlobalConfig.TermNumberNotFound);
            return currentMiners;
        }
        
        [View]
        public string GetCurrentMinersToFriendlyString(string empty)
        {
            return GetCurrentMiners(empty).ToString();
        }

        [View]
        public Tickets GetTicketsInfo(string publicKey)
        {
            Api.Assert(Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets), GlobalConfig.TicketsNotFound);
            return tickets;
        }
        
        [View]
        public string GetTicketsInfoToFriendlyString(string publicKey)
        {
            return GetTicketsInfo(publicKey).ToString();
        }

        /// <summary>
        /// Order by:
        /// 0 - Announcement order. (Default)
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        [View]
        public TicketsDictionary GetCurrentElectionInfo(int startIndex = 0, int length = 0, int orderBy = 0)
        {
            if (orderBy == 0)
            {
                var publicKeys = Collection.CandidatesField.GetValue().PublicKeys;
                if (length == 0)
                {
                    length = publicKeys.Count;
                }
                var dict = new Dictionary<string, Tickets>();
                foreach (var publicKey in publicKeys.Skip(startIndex).Take(length - startIndex))
                {
                    if (Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                    {
                        dict.Add(publicKey, tickets);
                    }
                }

                return dict.ToTicketsDictionary();
            }

            return new Dictionary<string, Tickets>().ToTicketsDictionary();
        }
        
        [View]
        public string GetCurrentElectionInfoToFriendlyString(int startIndex = 0, int length = 0, int orderBy = 0)
        {
            return GetCurrentElectionInfo(startIndex, length, orderBy).ToString();
        }
        
        [View]
        public ulong GetBlockchainAge(string empty)
        {
            return Collection.AgeField.GetValue();
        }

        [View]
        public StringList GetCurrentVictories(string empty)
        {
            return Process.GetVictories().ToStringList();
        }
        
        [View]
        public string GetCurrentVictoriesToFriendlyString(string empty)
        {
            return GetCurrentVictories(empty).ToString();
        }
  
        [View]
        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            Api.Assert(Collection.SnapshotField.TryGet(termNumber.ToUInt64Value(), out var snapshot), GlobalConfig.TermSnapshotNotFound);
            return snapshot;
        }
        
        [View]
        public string GetTermSnapshotToFriendlyString(ulong termNumber)
        {
            return GetTermSnapshot(termNumber).ToString();
        }

        [View]
        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            var map = Collection.TermNumberLookupField.GetValue().Map;
            Api.Assert(map != null, GlobalConfig.TermNumberLookupNotFound);
            return map?.OrderBy(p => p.Key).First(p => roundNumber >= p.Value).Key ?? (ulong) 0;
        }
        
        [View]
        public ulong GetVotesCount(string empty)
        {
            return Collection.VotesCountField.GetValue();
        }

        [View]
        public ulong GetTicketsCount(string empty)
        {
            return Collection.TicketsCountField.GetValue();
        }

        [View]
        public ulong QueryCurrentDividendsForVoters(string empty)
        {
            return Collection.RoundsMap.TryGet(GetCurrentRoundNumber(empty).ToUInt64Value(), out var roundInfo)
                ? Config.GetDividendsForVoters(roundInfo.GetMinedBlocks())
                : 0;
        }

        [View]
        public ulong QueryCurrentDividends(string empty)
        {
            return Collection.RoundsMap.TryGet(GetCurrentRoundNumber(empty).ToUInt64Value(), out var roundInfo)
                ? Config.GetDividendsForAll(roundInfo.GetMinedBlocks())
                : 0;
        }

        [View]
        public StringList QueryAliasesInUse(string empty)
        {
            var candidates = Collection.CandidatesField.GetValue();
            var result = new StringList();
            foreach (var publicKey in candidates.PublicKeys)
            {
                if (Collection.AliasesMap.TryGet(publicKey.ToStringValue(), out var alias))
                {
                    result.Values.Add(alias.Value);
                }
            }

            return result;
        }

        [View]
        public ulong QueryMinedBlockCountInCurrentTerm(string publicKey)
        {
            if (Collection.RoundsMap.TryGet(Api.GetCurrentRoundNumber().ToUInt64Value(), out var round))
            {
                if (round.RealTimeMinersInfo.ContainsKey(publicKey))
                {
                    return round.RealTimeMinersInfo[publicKey].ProducedBlocks;
                }
            }

            return 0;
        }
        
        [View]
        public string QueryAliasesInUseToFriendlyString(string empty)
        {
            return QueryAliasesInUse(empty).ToString();
        }
        
        public void AnnounceElection(string alias)
        {
            Election.AnnounceElection(alias);
        }

        public void QuitElection(string empty)
        {
            Election.QuitElection();
        }

        public void Vote(string candidatePublicKey, ulong amount, int lockTime)
        {
            Election.Vote(candidatePublicKey, amount, lockTime);
        }

        public void ReceiveDividendsByVotingDetail(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.ReceiveDividends(candidatePublicKey, amount, lockDays);
        }

        public void ReceiveDividendsByTransactionId(Hash transactionId)
        {
            Election.ReceiveDividends(transactionId);
        }
        
        public void ReceiveAllDividends(string empty)
        {
            Election.ReceiveDividends();
        }
        
        public void WithdrawByDetail(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.Withdraw(candidatePublicKey, amount, lockDays);
        }
        
        public void WithdrawByTransactionId(Hash transactionId)
        {
            Election.Withdraw(transactionId);
        }

        public void WithdrawAll(string empty)
        {
            Election.Withdraw();
        }
        
        #endregion Election
        
        #region Validation

        public BlockValidationResult ValidateBlock(BlockAbstract blockAbstract)
        {
            return Validation.ValidateBlock(blockAbstract);
        }
        
        #endregion Validation
    }
}