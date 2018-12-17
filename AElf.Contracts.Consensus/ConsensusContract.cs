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
            CurrentTermNumberField= new UInt64Field(GlobalConfig.AElfDPoSCurrentTermNumber),
            BlockchainStartTimestamp= new PbField<Timestamp>(GlobalConfig.AElfDPoSBlockchainStartTimestamp),

            RoundsMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSRoundsMapString),
            MinersMap = new Map<UInt64Value, Miners>(GlobalConfig.AElfDPoSMinersMapString),
            TicketsMap = new Map<StringValue, Tickets>(GlobalConfig.AElfDPoSTicketsMapString),
            SnapshotField = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotFieldString),
            AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString),
            HistoryMap = new Map<StringValue, CandidateInHistory>(GlobalConfig.AElfDPoSHistoryMapString),
        };

        private Process Process => new Process(Collection);

        private Election Election => new Election(Collection);

        #region Process
        
        [View]
        public Round GetRoundInfo(ulong roundNumber)
        {
            Api.Assert(Collection.RoundsMap.TryGet(roundNumber.ToUInt64Value(), out var roundInfo), GlobalConfig.RoundNumberNotFound);
            return roundInfo;
        }

        [View]
        public ulong GetCurrentRoundNumber()
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
        
        #endregion

        #region Election
        
        [View]
        public ulong GetCurrentTermNumber()
        {
            return Collection.CurrentTermNumberField.GetValue();
        }

        [View]
        public bool IsCandidate(string publicKey)
        {
            return Collection.CandidatesField.GetValue().PublicKeys.Contains(publicKey);
        }
        
        [View]
        public List<string> GetCandidatesList()
        {
            return Collection.CandidatesField.GetValue().PublicKeys.ToList();
        }

        [View]
        public CandidateInHistory GetCandidateHistoryInfo(string publicKey)
        {
            Api.Assert(Collection.HistoryMap.TryGet(publicKey.ToStringValue(), out var info),
                GlobalConfig.CandidateNotFound);
            return info;
        }

        [View]
        public List<string> GetCurrentMiners()
        {
            var currentTermNumber = Collection.CurrentTermNumberField.GetValue();
            Api.Assert(Collection.MinersMap.TryGet(currentTermNumber.ToUInt64Value(), out var currentMiners),
                GlobalConfig.TermNumberNotFound);
            return currentMiners.PublicKeys.ToList();
        }

        [View]
        public Tickets GetTicketsInfo(string publicKey)
        {
            Api.Assert(Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets), GlobalConfig.TicketsNotFound);
            return tickets;
        }

        [View]
        public Dictionary<string, Tickets> GetCurrentElectionInfo()
        {
            var dict = new Dictionary<string, Tickets>();
            foreach (var publicKey in Collection.CandidatesField.GetValue().PublicKeys)
            {
                if (Collection.TicketsMap.TryGet(publicKey.ToStringValue(), out var tickets))
                {
                    dict.Add(publicKey, tickets);
                }
            }

            return dict;
        }
        
        [View]
        public ulong GetBlockchainAge()
        {
            return Collection.AgeField.GetValue();
        }

        [View]
        public string GetCurrentVictories()
        {
            return Process.GetCurrentVictories();
        }
  
        [View]
        public TermSnapshot GetTermSnapshot(ulong termNumber)
        {
            Api.Assert(Collection.SnapshotField.TryGet(termNumber.ToUInt64Value(), out var snapshot), GlobalConfig.TermSnapshotNotFound);
            return snapshot;
        }

        [View]
        public ulong GetTermNumberByRoundNumber(ulong roundNumber)
        {
            var map = Collection.TermNumberLookupField.GetValue().Map;
            Api.Assert(map != null, GlobalConfig.TermNumberLookupNotFound);
            return map?.OrderBy(p => p.Key).First(p => roundNumber >= p.Value).Key ?? (ulong) 0;
        }
        
        public void AnnounceElection()
        {
            Election.AnnounceElection();
        }

        public void QuitElection()
        {
            Election.QuitElection();
        }

        public void Vote(string candidatePublicKey, ulong amount, int lockAmount)
        {
            Election.Vote(candidatePublicKey, amount, lockAmount);
        }

        public void GetDividendsByDetail(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.GetDividends(candidatePublicKey, amount, lockDays);
        }

        public void GetDividendsByTransactionId(Hash transactionId)
        {
            Election.GetDividends(transactionId);
        }
        
        public void GetAllDividends()
        {
            Election.GetDividends();
        }
        
        public void WithdrawByDetail(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.Withdraw(candidatePublicKey, amount, lockDays);
        }
        
        public void WithdrawByTransactionId(Hash transactionId)
        {
            Election.Withdraw(transactionId);
        }

        public void WithdrawAll()
        {
            Election.Withdraw();
        }
        
        #endregion
    }
}