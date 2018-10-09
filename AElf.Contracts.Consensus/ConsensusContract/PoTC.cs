using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Enums;
using AElf.Common;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types;

namespace AElf.Contracts.Consensus.ConsensusContract
{
    // ReSharper disable once InconsistentNaming
    public class PoTC : IConsensus
    {
        public ConsensusType Type => ConsensusType.PoTC;

        public ulong CurrentRoundNumber => 1;

        public int Interval => 0;

        public int LogLevel { get; set; }

        public Hash Nonce { get; set; } = Hash.Default;
        
        public Task Initialize(List<byte[]> args)
        {
            Console.WriteLine($"This message come from Proof of Transactions Count consensus - {nameof(Initialize)}");
            return Task.CompletedTask;
        }

        public Task Update(List<byte[]> args)
        {
            Console.WriteLine($"This message come from Proof of Transactions Count consensus - {nameof(Update)}");
            return Task.CompletedTask;
        }

        public Task Publish(List<byte[]> args)
        {
            Console.WriteLine($"This message come from Proof of Transactions Count consensus - {nameof(Publish)}");
            return Task.CompletedTask;
        }

        public Task<bool> Validation(List<byte[]> args)
        {
            Console.WriteLine($"This message come from Proof of Transactions Count consensus - {nameof(Validation)}");
            return Task.FromResult(true);
        }
    }
}