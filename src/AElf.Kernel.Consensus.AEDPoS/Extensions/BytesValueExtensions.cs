using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS
{
    public static class BytesValueExtensions
    {
        internal static ConsensusCommand ToConsensusCommand(this BytesValue bytesValue)
        {
            var consensusCommand = new ConsensusCommand();
            consensusCommand.MergeFrom(bytesValue.Value);
            return consensusCommand;
        }
    }
}