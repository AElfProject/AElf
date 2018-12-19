using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public interface IConsensus
    {
        void Start(bool willToMine);
        void DisposeConsensusEventList();
        Task UpdateConsensusInformation();
        bool IsAlive();
        bool Shutdown();
    }    
}