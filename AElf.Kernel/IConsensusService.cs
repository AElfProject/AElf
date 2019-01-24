using System;

namespace AElf.Kernel
{
    public interface IConsensusService
    {
        IDisposable ConsensusObservables { get; set; }
        
        
    }
}