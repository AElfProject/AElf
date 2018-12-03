using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public interface IConsensus
    {
        Task Start();
        void DisposeConsensusList();
        Task UpdateConsensusEventList();
        bool IsAlive();
        bool Shutdown();
    }    
}