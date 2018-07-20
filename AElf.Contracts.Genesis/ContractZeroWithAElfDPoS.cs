using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ContractZeroWithAElfDPoS : BasicContractZero
    {
        public readonly UInt64Field CurrentRoundNumberField = new UInt64Field(Globals.AElfDPoSCurrentRoundNumber);

        public readonly PbField<BlockProducer> BlockProducerField =
            new PbField<BlockProducer>(Globals.AElfDPoSBlockProducerString);

        // ReSharper disable once InconsistentNaming
        public readonly Map<UInt64Value, RoundInfo> DPoSInfoMap =
            new Map<UInt64Value, RoundInfo>(Globals.AElfDPoSInformationString);
        
        // ReSharper disable once InconsistentNaming
        public readonly Map<UInt64Value, StringValue> EBPMap =
            new Map<UInt64Value, StringValue>(Globals.AElfDPoSExtraBlockProducerString);

        public readonly PbField<Timestamp> TimeForProducingExtraBlock =
            new PbField<Timestamp>(Globals.AElfDPoSExtraBlockTimeslotString);

        public readonly Map<UInt64Value, StringValue> FirstPlaceMap
            = new Map<UInt64Value, StringValue>(Globals.AElfDPoSFirstPlaceOfEachRoundString);
        
        private IConsensus _consensus;
        
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public async Task InitializeAElfDPoS(BlockProducer blockProducer, DPoSInfo dPoSInfo)
        {
            _consensus = new AElfDPoS(this);
            await _consensus.Initialize(new List<byte[]> {blockProducer.ToByteArray(), dPoSInfo.ToByteArray()});
        }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public async Task UpdateAElfDPoS(ByteString currentRoundInfo, ByteString nextRoundInfo, ByteString nextExtraBlockProducer)
        {
            await _consensus.Update(new List<byte[]>
            {
                currentRoundInfo.ToArray(),
                nextRoundInfo.ToArray(),
                nextExtraBlockProducer.ToArray()
            });
        }

        // ReSharper disable once UnusedMember.Global
        public async Task<BoolValue> Validation(ByteString accountAddress, ByteString timestamp)
        {
            return new BoolValue
            {
                Value = await _consensus.Validation(new List<byte[]> {accountAddress.ToArray(), timestamp.ToArray()})
            };
        }

        // ReSharper disable once UnusedMember.Global
        public async Task PublishOutValueAndSignature(ByteString roundNumber, ByteString accountAddress, ByteString outValue, ByteString signature)
        {
            await _consensus.Publish(new List<byte[]>
            {
                roundNumber.ToArray(),
                accountAddress.ToArray(),
                outValue.ToArray(),
                signature.ToArray()
            });
        }
        
        // ReSharper disable once UnusedMember.Global
        public async Task PublishInValue(ByteString roundNumber, ByteString accountAddress, ByteString inValue)
        {
            await _consensus.Publish(new List<byte[]>
            {
                roundNumber.ToArray(),
                accountAddress.ToArray(),
                inValue.ToArray()
            });
        }
    }
}