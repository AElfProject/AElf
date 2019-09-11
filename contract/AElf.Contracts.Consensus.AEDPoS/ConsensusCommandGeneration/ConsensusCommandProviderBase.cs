using Acs4;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public class ConsensusCommandProviderBase : IConsensusCommandProvider
        {
            private readonly AElfConsensusBehaviour _behaviour;

            /// <summary>
            /// No, you can't mine blocks.
            /// </summary>
            public static ConsensusCommand InvalidConsensusCommand => new ConsensusCommand
            {
                ExpectedMiningTime = new Timestamp {Seconds = long.MaxValue},
                Hint = ByteString.CopyFrom(new AElfConsensusHint
                {
                    Behaviour = AElfConsensusBehaviour.Nothing
                }.ToByteArray()),
                LimitMillisecondsOfMiningBlock = 0,
                NextBlockMiningLeftMilliseconds = int.MaxValue
            };

            public ConsensusCommandProviderBase(AElfConsensusBehaviour behaviour)
            {
                _behaviour = behaviour;
            }

            public ConsensusCommand GetConsensusCommand()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}