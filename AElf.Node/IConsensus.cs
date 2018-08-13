using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Node
{
    public interface IConsensus
    {
        IDisposable ConsensusDisposable { get; set; }
        Task Start();
        Task Update();
        Task RecoverMining();
    }
}