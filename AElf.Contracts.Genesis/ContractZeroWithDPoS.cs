using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class ContractZeroWithDPoS : BasicContractZero
    {
        private readonly IConsensus _consensus = new AElfDPoS();
        
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once InconsistentNaming
        public async Task InitializeAElfDPoS(ByteString blockProducer, ByteString dPoSInfo)
        {
            await _consensus.Initialize(new List<byte[]> {blockProducer.ToArray(), dPoSInfo.ToArray()});
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