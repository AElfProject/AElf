using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    internal class AEDPoSExtraDataParsingService : IConsensusExtraDataParsingService
    {
        public BytesValue ParseHeaderExtraData(byte[] extraData)
        {
            return BytesValue.Parser.ParseFrom(extraData);
        }
    }
}