using System;

namespace AElf.Kernel
{
    public class ConsensusService : IConsensusService
    {
        public IDisposable ConsensusObservables { get; set; }
    }
}