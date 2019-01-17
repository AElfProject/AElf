using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using Volo.Abp;

namespace AElf.Kernel.Managers
{
    public interface ISecondChainManager
    {
    }

    public class SecondChainManager : ISecondChainManager
    {
        private readonly BlockchainStore<Chain> _chains;

        public SecondChainManager(BlockchainStore<Chain> chains)
        {
            _chains = chains;
        }

        public async Task<Chain> CreateAsync(long chainId, Hash genesisBlock)
        {
            var chain =  await _chains.GetAsync(chainId.ToHex());
            if(chain != null)
                throw new InvalidOperationException("chain already exists");
            
            chain = new Chain()
            {
                BestChainHash = genesisBlock,
                GenesisBlockHash = genesisBlock,
            };

            return chain;
        }

    }
}