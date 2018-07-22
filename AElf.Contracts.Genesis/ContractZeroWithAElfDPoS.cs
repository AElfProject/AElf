using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Consensus;
using AElf.Contracts.Genesis.ConsensusContract;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ClassNeverInstantiated.Global
    // ReSharper disable once UnusedMember.Global
    public class ContractZeroWithAElfDPoS : BasicContractZero
    {
        private readonly IConsensus _consensus = new AElfDPoS(new AElfDPoSFiledMapCollection
        {
            CurrentRoundNumberField = new UInt64Field(Globals.AElfDPoSCurrentRoundNumber),
            BlockProducerField = new PbField<BlockProducer>(Globals.AElfDPoSBlockProducerString),
            DPoSInfoMap = new Map<UInt64Value, RoundInfo>(Globals.AElfDPoSInformationString),
            EBPMap = new Map<UInt64Value, StringValue>(Globals.AElfDPoSExtraBlockProducerString),
            TimeForProducingExtraBlockField = new PbField<Timestamp>(Globals.AElfDPoSExtraBlockTimeslotString),
            FirstPlaceMap = new Map<UInt64Value, StringValue>(Globals.AElfDPoSFirstPlaceOfEachRoundString)
        });
        
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public async Task InitializeAElfDPoS(byte[] blockProducer, byte[] dPoSInfo)
        {
            await _consensus.Initialize(new List<byte[]> {blockProducer, dPoSInfo});
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public async Task UpdateAElfDPoS(byte[] currentRoundInfo, byte[] nextRoundInfo, byte[] nextExtraBlockProducer)
        {
            await _consensus.Update(new List<byte[]>
            {
                currentRoundInfo,
                nextRoundInfo,
                nextExtraBlockProducer
            });
        }

        // ReSharper disable once UnusedMember.Global
        public async Task<BoolValue> Validation(byte[] accountAddress, byte[] timestamp)
        {
            return new BoolValue
            {
                Value = await _consensus.Validation(new List<byte[]> {accountAddress, timestamp})
            };
        }

        // ReSharper disable once UnusedMember.Global
        public async Task PublishOutValueAndSignature(byte[] roundNumber, byte[] accountAddress, byte[] outValue, byte[] signature)
        {
            await _consensus.Publish(new List<byte[]>
            {
                roundNumber,
                accountAddress,
                outValue,
                signature
            });
        }
        
        // ReSharper disable once UnusedMember.Global
        public async Task PublishInValue(byte[] roundNumber, byte[] accountAddress, byte[] inValue)
        {
            await _consensus.Publish(new List<byte[]>
            {
                roundNumber,
                accountAddress,
                inValue
            });
        }
    }
}