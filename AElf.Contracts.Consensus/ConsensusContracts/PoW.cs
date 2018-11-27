using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Enums;
using AElf.Common;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types;

namespace AElf.Contracts.Consensus.ConsensusContracts
{
    public class PoW : IConsensus
    {
        public ConsensusType Type => ConsensusType.PoW;

        public ulong CurrentRoundNumber => 1;

        public int Interval => 0;

        public int LogLevel { get; set; }

        public Hash Nonce { get; set; } = Hash.Zero;

        public Task Initialize(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }

        public Task Update(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task Publish(List<byte[]> args)
        {
            
        }

        public Task<int> Validation(List<byte[]> args)
        {
            throw new System.NotImplementedException();
        }
    }
}