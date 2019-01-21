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
        private readonly IBlockchainStore<Chain> _chains;
        private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;

        public SecondChainManager(IBlockchainStore<Chain> chains,
            IBlockchainStore<ChainBlockLink> chainBlockLinks)
        {
            _chains = chains;
            _chainBlockLinks = chainBlockLinks;
        }

        public async Task<Chain> CreateAsync(int chainId, Hash genesisBlock)
        {
            var chain = await _chains.GetAsync(chainId.ToHex());
            if (chain != null)
                throw new InvalidOperationException("chain already exists");

            chain = new Chain()
            {
                BestChainHash = genesisBlock,
                GenesisBlockHash = genesisBlock,
            };

            return chain;
        }

        public async Task<ChainBlockLink> AttachBlockToChain(int chainId, ChainBlockLink chainBlockLink)
        {
            var chain = await _chains.GetAsync(chainId.ToHex());
            if (chain == null)
                throw new InvalidOperationException("Chain not exists");

            await _chainBlockLinks.SetAsync(
                chainId.ToHex() + chainBlockLink.BlockHash.ToHex(), chainBlockLink);

            while (true)
            {
                var previousHash = chainBlockLink.PreviousBlockHash.ToHex();
                var blockHash = chainBlockLink.BlockHash.ToHex();

                if (chain.Branches.ContainsKey(previousHash))
                {
                    chain.Branches[blockHash] = chainBlockLink.Height;
                    chain.Branches.Remove(previousHash);

                    if (!chain.NotLinkedBlocks.ContainsKey(blockHash))
                    {
                        break;
                    }

                    chainBlockLink = await _chainBlockLinks.GetAsync(
                        chainId.ToHex() + chain.NotLinkedBlocks[blockHash]);
                    chain.NotLinkedBlocks.Remove(blockHash);
                }
                else
                {
                    if (chain.BestChainHeight < chainBlockLink.Height)
                        throw new InvalidOperationException(
                            "Found a block is lower than the best block but no link");

                    chain.NotLinkedBlocks[previousHash] = blockHash;
                    break;
                }
            }

            await _chains.SetAsync(chainId.ToHex(), chain);


            return chainBlockLink;
        }
    }
}