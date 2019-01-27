using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.Managers.Another
{
    [Flags]
    public enum BlockAttchOperationStatus
    {
        None = 0,
        NewBlockNotLinked = 1 << 1,
        NewBlockLinked = 1 << 2,
        BestChainFound = 1 << 3 | NewBlockLinked,
        NewBlocksLinked = 1 << 4 | NewBlockLinked
    }

    public interface IChainManager
    {
    }


    public class ChainManager : IChainManager, ISingletonDependency
    {
        private readonly IBlockchainStore<Chain> _chains;
        private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;

        public ChainManager(IBlockchainStore<Chain> chains,
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
                Branches =
                {
                    {genesisBlock.ToHex(), 0}
                }
            };

            return chain;
        }

        public async Task<Chain> GetAsync(int chainId)
        {
            var chain = await _chains.GetAsync(chainId.ToHex());
            return chain;
        }

        public async Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, Hash blockHash)
        {
            return await GetChainBlockLinkAsync(chainId, blockHash.ToHex());
        }

        public async Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, string blockHash)
        {
            return await _chainBlockLinks.GetAsync(chainId.ToHex() + blockHash);
        }

        public async Task SetChainBlockLinkAsync(int chainId, ChainBlockLink chainBlockLink)
        {
            await _chainBlockLinks.SetAsync(chainId.ToHex() + chainBlockLink.BlockHash.ToHex(), chainBlockLink);
        }

        public async Task<BlockAttchOperationStatus> AttachBlockToChain(Chain chain, ChainBlockLink chainBlockLink)
        {
            BlockAttchOperationStatus status = BlockAttchOperationStatus.None;

            while (true)
            {
                var previousHash = chainBlockLink.PreviousBlockHash.ToHex();
                var blockHash = chainBlockLink.BlockHash.ToHex();

                if (chain.Branches.ContainsKey(previousHash))
                {
                    chain.Branches[blockHash] = chainBlockLink.Height;
                    chain.Branches.Remove(previousHash);

                    if (chainBlockLink.Height > chain.BestChainHeight)
                    {
                        chain.BestChainHeight = chainBlockLink.Height;
                        chain.BestChainHash = chainBlockLink.BlockHash;
                        status |= BlockAttchOperationStatus.BestChainFound;
                    }


                    if (chainBlockLink.IsLinked)
                        throw new Exception("chain block link should not be linked");

                    chainBlockLink.IsLinked = true;
                    
                    await SetChainBlockLinkAsync(chain.Id, chainBlockLink);
                    
                    if (!chain.NotLinkedBlocks.ContainsKey(blockHash))
                    {
                        status |= BlockAttchOperationStatus.NewBlockLinked;
                        break;
                    }

                    chainBlockLink = await GetChainBlockLinkAsync(
                        chain.Id, chain.NotLinkedBlocks[blockHash]);

                    chain.NotLinkedBlocks.Remove(blockHash);

                    status |= BlockAttchOperationStatus.NewBlocksLinked;
                }
                else
                {
                    if (chainBlockLink.Height <= chain.BestChainHeight)
                    {
                        //check database to ensure whether it can be a branch
                        var previousChainBlockLink =
                            await this.GetChainBlockLinkAsync(chain.Id, chainBlockLink.PreviousBlockHash);
                        if (previousChainBlockLink.IsLinked)
                        {
                            chain.Branches[previousChainBlockLink.BlockHash.ToHex()] = previousChainBlockLink.Height;
                            continue;
                        }
                    }

                    chain.NotLinkedBlocks[previousHash] = blockHash;

                    if (status != BlockAttchOperationStatus.None)
                        throw new Exception("invalid status");

                    status = BlockAttchOperationStatus.NewBlockNotLinked;
                    await SetChainBlockLinkAsync(chain.Id, chainBlockLink);
                    break;
                }
            }

            await _chains.SetAsync(chain.Id.ToHex(), chain);


            return status;
        }
    }
}