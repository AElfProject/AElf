using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public interface IConsensus
    {
        Task Start();
        void Stop();
        void IncrementLockNumber();
        void DecrementLockNumber();
        Task Update();
        bool IsAlive();
        bool Shutdown();
        void FillConsensusWithKeyPair();
    }    
}