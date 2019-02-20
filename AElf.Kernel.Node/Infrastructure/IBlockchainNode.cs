using System;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool.Infrastructure;

namespace AElf.Kernel.Node.Infrastructure
{
    public interface IBlockchainNode
    {
        Task StartAsync(int chainId);
        Task StopAsync();
    }

    /*
    public interface IBlockchainNodeComponent
    {
        Task StartAsync(int chainId);
        Task StopAsync();
    }*/
    
    public class BlockchainNode: IBlockchainNode
    {
        public async Task StartAsync(int chainId)
        {
            throw new NotImplementedException();
        }

        public async Task StopAsync()
        {
            throw new NotImplementedException();
        }
    }
}