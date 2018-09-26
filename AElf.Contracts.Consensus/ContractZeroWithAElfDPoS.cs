using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.ConsensusContract;
using AElf.Contracts.Consensus.ConsensusContract.FieldMapCollections;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public class ContractZeroWithAElfDPoS : CSharpSmartContract
    {
        private readonly IConsensus _consensus = new DPoS(new AElfDPoSFiledMapCollection
        {
            CurrentRoundNumberField = new UInt64Field(Globals.AElfDPoSCurrentRoundNumber),
            BlockProducerField = new PbField<Miners>(Globals.AElfDPoSBlockProducerString),
            DPoSInfoMap = new Map<UInt64Value, Round>(Globals.AElfDPoSInformationString),
            EBPMap = new Map<UInt64Value, StringValue>(Globals.AElfDPoSExtraBlockProducerString),
            TimeForProducingExtraBlockField = new PbField<Timestamp>(Globals.AElfDPoSExtraBlockTimeSlotString),
            FirstPlaceMap = new Map<UInt64Value, StringValue>(Globals.AElfDPoSFirstPlaceOfEachRoundString),
            MiningIntervalField = new Int32Field(Globals.AElfDPoSMiningIntervalString),
            RoundHashMap = new Map<UInt64Value, Int64Value>(Globals.AElfDPoSMiningRoundHashMapString)
        });

        public async Task InitializeAElfDPoS(byte[] blockProducer, byte[] dPoSInfo, byte[] miningInterval,
            byte[] logLevel)
        {
            await _consensus.Initialize(new List<byte[]> {blockProducer, dPoSInfo, miningInterval, logLevel});
        }

        public async Task UpdateAElfDPoS(byte[] currentRoundInfo, byte[] nextRoundInfo, byte[] nextExtraBlockProducer)
        {
            await _consensus.Update(new List<byte[]>
            {
                currentRoundInfo,
                nextRoundInfo,
                nextExtraBlockProducer
            });
        }

        public async Task<BoolValue> Validation(byte[] accountAddress, byte[] timestamp)
        {
            return new BoolValue
            {
                Value = await _consensus.Validation(new List<byte[]> {accountAddress, timestamp})
            };
        }

        public async Task PublishOutValueAndSignature(byte[] roundNumber, byte[] accountAddress, byte[] outValue,
            byte[] signature, byte[] roundId)
        {
            await _consensus.Publish(new List<byte[]>
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
            await _consensus.Publish(new List<byte[]>
            {
                roundNumber,
                accountAddress,
                inValue,
                roundId
            });
        }
    }
}