using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public interface IConsensus
    {
        void Start(bool willToMine);
        void DisposeConsensusList();
        Task UpdateConsensusEventList();
        bool IsAlive();
        bool Shutdown();
    }    
}