using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.AEDPoS
{
    public static class ByteStringExtensions
    {
        internal static AElfConsensusHint ToAElfConsensusHint(this ByteString byteString)
        {
            var hint = new AElfConsensusHint();
            hint.MergeFrom(byteString);
            return hint;
        }
    }
}