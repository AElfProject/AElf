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
            AgeField = new UInt64Field(GlobalConfig.AElfDPoSAgeFieldString),
            CurrentTermNumberField= new UInt64Field(GlobalConfig.AElfDPoSCurrentTermNumber),
            BlockchainStartTimestamp= new PbField<Timestamp>(GlobalConfig.AElfDPoSBlockchainStartTimestamp),

            RoundsMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSRoundsMapString),
            TicketsMap = new Map<StringValue, Tickets>(GlobalConfig.AElfDPoSTicketsMapString),
            SnapshotField = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotFieldString),
            AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString),
            HistoryMap = new Map<StringValue, CandidateInHistory>(GlobalConfig.AElfDPoSHistoryMapString),
            TermKeyLookUpMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSTermLookUpString)
        };

        private Process Process => new Process(Collection);

        private Election Election => new Election(Collection);

        #region Process
        
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
            Process.Update(forwarding);
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
        
        public void AnnounceElection()
        {
            Election.AnnounceElection();
        }

        public void QuitElection()
        {
            Election.QuitElection();
        }

        public void Vote(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.Vote(candidatePublicKey, amount, lockDays);
        }

        public void GetDividends(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.GetDividends(candidatePublicKey, amount, lockDays);
        }

        public void GetDividends(Hash transactionId)
        {
            Election.GetDividends(transactionId);
        }
        
        public void GetDividends()
        {
            Election.GetDividends();
        }
        
        public void Withdraw(string candidatePublicKey, ulong amount, int lockDays)
        {
            Election.Withdraw(candidatePublicKey, amount, lockDays);
        }
        
        public void Withdraw(Hash transactionId)
        {
            Election.Withdraw(transactionId);
        }

        public void Withdraw()
        {
            Election.Withdraw();
        }
        
        [View]
        public List<string> GetCurrentMiners()
        {
            var currentRoundNumber = Collection.CurrentRoundNumberField.GetValue();
            Api.Assert(currentRoundNumber != 0, "DPoS process hasn't started yet.");
            if (Collection.RoundsMap.TryGet(currentRoundNumber.ToUInt64Value(), out var currentRoundInfo))
            {
                var realTimeMiners = currentRoundInfo.RealTimeMinersInfo;
                return realTimeMiners.Keys.ToList();
            }
            
            return new List<string>();
        }
        
        #endregion
    }
}