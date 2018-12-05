using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public class ConsensusContract : CSharpSmartContract
    {
        #region DPoS

        private AElfDPoSFieldMapCollection Collection => new AElfDPoSFieldMapCollection
        {
            CurrentRoundNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentRoundNumber),
            OngoingMinersField = new PbField<OngoingMiners>(GlobalConfig.AElfDPoSOngoingMinersString),
            TimeForProducingExtraBlockField = new PbField<Timestamp>(GlobalConfig.AElfDPoSExtraBlockTimeSlotString),
            MiningIntervalField = new Int32Field(GlobalConfig.AElfDPoSMiningIntervalString),
            CandidatesField = new PbField<Candidates>(GlobalConfig.AElfDPoSCandidatesString),

            DPoSInfoMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSInformationString),
            EBPMap = new Map<UInt64Value, BytesValue>(GlobalConfig.AElfDPoSExtraBlockProducerString),
            FirstPlaceMap = new Map<UInt64Value, BytesValue>(GlobalConfig.AElfDPoSFirstPlaceOfEachRoundString),
            BalanceMap = new Map<BytesValue, Tickets>(GlobalConfig.AElfDPoSBalanceMapString),
            SnapshotField = new Map<UInt64Value, ElectionSnapshot>(GlobalConfig.AElfDPoSSnapshotFieldString),
            DividendsMap = new Map<UInt64Value, UInt64Value>(GlobalConfig.AElfDPoSDividendsMapString),
            AliasesMap = new Map<BytesValue, StringValue>(GlobalConfig.AElfDPoSAliasesMapString)
        };

        private Process Process => new Process(Collection);

        private Election Election => new Election(Collection);

        public void InitializeAElfDPoS(Miners miners, AElfDPoSInformation dpoSInformation, int miningInterval,
            int logLevel)
        {
            Process.Initialize(miners, dpoSInformation, miningInterval, logLevel);
        }

        public void UpdateAElfDPoS(Round currentRoundInfo, Round nextRoundInfo, string nextExtraBlockProducer,
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

        public void ReElection(byte[] roundNumber)
        {

        }

        public void Complain()
        {

        }

        [View]
        public Miners GetCurrentMiners()
        {
            var currentRoundNumber = Collection.CurrentRoundNumberField.GetValue();
            Api.Assert(currentRoundNumber != 0, "DPoS process hasn't started yet.");
            return Collection.OngoingMinersField.GetValue().GetCurrentMiners(currentRoundNumber);
        }

        public void Withdraw(byte[] pubKey, ulong amount)
        {
        }

        #endregion
    }
}