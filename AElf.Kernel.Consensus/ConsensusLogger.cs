using AElf.Kernel.EventMessages;
using Easy.MessageHub;

namespace AElf.Kernel.Consensus
{
    public class ConsensusLogger
    {
        private readonly ConsensusDataReader _reader;

        public ConsensusLogger(ConsensusDataReader reader)
        {
            _reader = reader;
            MessageHub.Instance.Subscribe<UpdateConsensus>(async option =>
            {
                if (option == UpdateConsensus.Update)
                {
                    
                }
            });
        }
    }
}