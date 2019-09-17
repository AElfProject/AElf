using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        protected class ConsensusCommandProvider : IConsensusCommandProvider
        {
            private readonly ICommandStrategy _commandStrategy;

            /// <summary>
            /// No, you can't mine blocks.
            /// </summary>
            public static ConsensusCommand InvalidConsensusCommand => new ConsensusCommand
            {
                ArrangedMiningTime = new Timestamp {Seconds = int.MaxValue},
                Hint = ByteString.CopyFrom(new AElfConsensusHint
                {
                    Behaviour = AElfConsensusBehaviour.Nothing
                }.ToByteArray()),
                LimitMillisecondsOfMiningBlock = 0,
                NextBlockMiningLeftMilliseconds = int.MaxValue
            };

            public ConsensusCommandProvider(ICommandStrategy commandStrategy)
            {
                _commandStrategy = commandStrategy;
            }

            public ConsensusCommand GetConsensusCommand()
            {
                return _commandStrategy.GetConsensusCommand();
            }
        }
    }
}