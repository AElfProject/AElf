using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public static class ByteStringExtensions
    {
        internal static AElfConsensusHeaderInformation ToConsensusHeaderInformation(this BytesValue bytesValue)
        {
            var headerInformation = new AElfConsensusHeaderInformation();
            headerInformation.MergeFrom(bytesValue.Value);
            return headerInformation;
        }
    }
}