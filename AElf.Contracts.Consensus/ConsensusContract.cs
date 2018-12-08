using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using ServiceStack.Templates;
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

            RoundsMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSRoundsMapString),
            TicketsMap = new Map<StringValue, Tickets>(GlobalConfig.AElfDPoSTicketsMapString),
            SnapshotField = new Map<UInt64Value, TermSnapshot>(GlobalConfig.AElfDPoSSnapshotFieldString),
            DividendsMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSDividendsMapString),
            AliasesMap = new Map<StringValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString)
        };

        private Process Process => new Process(Collection);

        private Election Election => new Election(Collection);

        #region Process
        
        public void InitialTerm(Term term, int logLevel)
        {
            Api.Assert(term.FirstRound.RoundNumber == 1);
            Api.Assert(term.SecondRound.RoundNumber == 2);
            
            Process.Initialize(term, logLevel);
        }
        
        public void NewTerm()
        {
            
        }

        public void UpdateConsensus(Round currentRoundInfo, Round nextRoundInfo, string nextExtraBlockProducer,
            long roundId)
        {
            Process.Update(currentRoundInfo, nextRoundInfo, nextExtraBlockProducer, roundId);
        }

        public void PublishOutValueAndSignature(ulong roundNumber, Hash outValue, Hash signature, long roundId)
        {
            Process.PublishOutValue(roundNumber, outValue, signature, roundId);
        }

        public void PublishInValue(ulong roundNumber, Hash inValue, long roundId)
        {
            Process.PublishInValue(roundNumber, inValue, roundId);
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

        public void Vote(byte[] candidatePubKey, ulong amount)
        {
            Election.Vote();
        }

        public void Complain()
        {

        }

        [View]
        public Miners GetCurrentMiners()
        {
            var currentRoundNumber = Collection.CurrentRoundNumberField.GetValue();
            Api.Assert(currentRoundNumber != 0, "DPoS process hasn't started yet.");
            return null;
        }

        public void Withdraw(byte[] pubKey, ulong amount)
        {
        }
        
        #endregion
    }
}