using System.Threading.Tasks;

namespace AElf.Network.Sim.Tests
{
    public interface INetworkTest
    {
        void Run();
        void StopAndClean();
    }
}