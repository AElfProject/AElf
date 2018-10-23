using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public interface IConsensus
    {
        Task Start();
        void Stop();
        void Hang();
        void Recover();
        Task Update();
        bool IsAlive();
    }
}