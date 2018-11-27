using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.ConsensusContracts;
using AElf.Contracts.Consensus.ConsensusContracts.FieldMapCollections;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public class ConsensusContractZero : CSharpSmartContract
    {
        #region DPoS
        private IConsensus DPoSConsensus => new DPoS(new AElfDPoSFieldMapCollection
        {
            CurrentRoundNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentRoundNumber),
            OngoingMinersField = new PbField<OngoingMiners>(GlobalConfig.AElfDPoSOngoingMinersString),
            TimeForProducingExtraBlockField = new PbField<Timestamp>(GlobalConfig.AElfDPoSExtraBlockTimeSlotString),
            MiningIntervalField = new Int32Field(GlobalConfig.AElfDPoSMiningIntervalString),
            CandidatesFiled = new PbField<Candidates>(GlobalConfig.AElfDPoSCandidatesString),
            
            DPoSInfoMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSInformationString),
            EBPMap = new Map<UInt64Value, StringValue>(GlobalConfig.AElfDPoSExtraBlockProducerString),
            FirstPlaceMap = new Map<UInt64Value, StringValue>(GlobalConfig.AElfDPoSFirstPlaceOfEachRoundString),
            RoundHashMap = new Map<UInt64Value, Int64Value>(GlobalConfig.AElfDPoSMiningRoundHashMapString),
            BalanceMap = new Map<Address, Tickets>(GlobalConfig.AElfDPoSBalanceMapString)
        });

        public async Task InitializeAElfDPoS(byte[] blockProducer, byte[] dPoSInfo, byte[] miningInterval,
            byte[] logLevel)
        {
            await DPoSConsensus.Initialize(new List<byte[]> {blockProducer, dPoSInfo, miningInterval, logLevel});
        }

        public async Task UpdateAElfDPoS(byte[] currentRoundInfo, byte[] nextRoundInfo, byte[] nextExtraBlockProducer)
        {
            await DPoSConsensus.Update(new List<byte[]>
            {
                currentRoundInfo,
                nextRoundInfo,
                nextExtraBlockProducer
            });
        }

        public async Task<Int32Value> Validation(byte[] accountAddress, byte[] timestamp, byte[] roundId)
        {
            return new Int32Value
            {
                Value = await DPoSConsensus.Validation(new List<byte[]> {accountAddress, timestamp, roundId})
            };
        }

        public async Task PublishOutValueAndSignature(byte[] roundNumber, byte[] accountAddress, byte[] outValue,
            byte[] signature, byte[] roundId)
        {
            await DPoSConsensus.Publish(new List<byte[]>
            {
                roundNumber,
                accountAddress,
                outValue,
                signature,
                roundId
            });
        }

        public async Task PublishInValue(byte[] roundNumber, byte[] accountAddress, byte[] inValue, byte[] roundId)
        {
            await DPoSConsensus.Publish(new List<byte[]>
            {
                roundNumber,
                accountAddress,
                inValue,
                roundId
            });
        }
        #endregion
    }
}