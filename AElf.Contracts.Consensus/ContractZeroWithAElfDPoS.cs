using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.ConsensusContract;
using AElf.Contracts.Consensus.ConsensusContract.FieldMapCollections;
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
    public class ContractZeroWithAElfDPoS : CSharpSmartContract
    {
        private readonly IConsensus _consensus = new DPoS(new AElfDPoSFieldMapCollection
        {
            CurrentRoundNumberField = new UInt64Field(GlobalConfig.AElfDPoSCurrentRoundNumber),
            OngoingMinersField = new PbField<OngoingMiners>(GlobalConfig.AElfDPoSOngoingMinersString),
            TimeForProducingExtraBlockField = new PbField<Timestamp>(GlobalConfig.AElfDPoSExtraBlockTimeSlotString),
            MiningIntervalField = new Int32Field(GlobalConfig.AElfDPoSMiningIntervalString),
            
            DPoSInfoMap = new Map<UInt64Value, Round>(GlobalConfig.AElfDPoSInformationString),
            EBPMap = new Map<UInt64Value, StringValue>(GlobalConfig.AElfDPoSExtraBlockProducerString),
            FirstPlaceMap = new Map<UInt64Value, StringValue>(GlobalConfig.AElfDPoSFirstPlaceOfEachRoundString),
            RoundHashMap = new Map<UInt64Value, Int64Value>(GlobalConfig.AElfDPoSMiningRoundHashMapString),
            BalanceMap = new Map<Address, UInt64Value>(GlobalConfig.AElfDPoSBalanceMapString)
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

        public async Task<Int32Value> Validation(byte[] accountAddress, byte[] timestamp, byte[] roundId)
        {
            return new Int32Value
            {
                Value = await _consensus.Validation(new List<byte[]> {accountAddress, timestamp, roundId})
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