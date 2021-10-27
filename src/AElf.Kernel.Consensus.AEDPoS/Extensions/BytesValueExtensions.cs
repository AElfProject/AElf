using AElf.Standards.ACS4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Consensus.AEDPoS
{
    internal static class BytesValueExtensions
    {
        internal static ConsensusCommand ToConsensusCommand(this BytesValue bytesValue)
        {
            var consensusCommand = new ConsensusCommand();
            consensusCommand.MergeFrom(bytesValue.Value);
            return consensusCommand;
        }
    }
}