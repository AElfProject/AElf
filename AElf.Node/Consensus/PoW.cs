using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    public class PoW : IConsensus
    {
        public Task Start()
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        public void IncrementLockNumber()
        {
            throw new System.NotImplementedException();
        }

        public void DecrementLockNumber()
        {
            throw new System.NotImplementedException();
        }

        public Task Update()
        {
            throw new System.NotImplementedException();
        }

        public bool IsAlive()
        {
            throw new System.NotImplementedException();
        }
        
        public bool Shutdown()
        {
            throw new System.NotImplementedException();
        }
    }
}