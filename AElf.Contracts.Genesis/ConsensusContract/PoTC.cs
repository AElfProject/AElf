using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types;

namespace AElf.Contracts.Genesis.ConsensusContract
{
    // ReSharper disable once InconsistentNaming
    public class PoTC : IConsensus
    {
        public ConsensusType Type => ConsensusType.PoTC;

        public ulong CurrentRoundNumber => 1;

        public int Interval => 0;

        public bool PrintLogs => true;

        public Hash Nonce { get; set; } = Hash.Default;
        
        public Task Initialize(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }

        public Task Publish(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> Validation(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }
    }
}