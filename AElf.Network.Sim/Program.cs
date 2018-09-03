using System.Threading;
using System.Threading.Tasks;
using AElf.Network.Sim.Tests.Peers;

namespace AElf.Network.Sim
{
    class Program
    {
        public static AutoResetEvent testSuccessEvent = new AutoResetEvent(false);
        
        static void Main(string[] args)
        {
            PeerDiscoveryTest test = new PeerDiscoveryTest();

            Task.Run(() => test.Run());

            testSuccessEvent.WaitOne();
            test.StopAndClean();
        }
    }
}