using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types.Comparers;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Domain
{
    [Flags]
    public enum BlockAttachOperationStatus
    {
        None = 0,
        NewBlockNotLinked = 1 << 1,
        NewBlockLinked = 1 << 2,
        LongestChainFound = 1 << 3 | NewBlockLinked,
        NewBlocksLinked = 1 << 4 | NewBlockLinked
    }

    public interface IChainManager
    {
        Task<Chain> CreateAsync(int chainId, Hash genesisBlock);
        Task<Chain> GetAsync(int chainId);
        Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, Hash blockHash);
        Task<ChainBlockIndex> GetChainBlockIndexAsync(int chainId, ulong blockHeight);

        Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain,
            ChainBlockLink chainBlockLink);

        Task<Hash> UpdateIrreversibleBlockAsync(Chain chain, Hash start, int confirmationsNeeded);
        Task SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash);

        Task<List<ChainBlockLink>> GetNotExecutedBlocks(int chainId, Hash blockHash);

        Task SetChainBlockLinkExecutionStatus(int chainId, ChainBlockLink blockLink,
            ChainBlockLinkExecutionStatus status);

        Task SetBestChainAsync(Chain chain, ulong bestChainHeight, Hash bestChainHash);
    }

    public class ChainManager : IChainManager, ISingletonDependency
    {
        private readonly IBlockchainStore<Chain> _chains;
        private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;
        private readonly IBlockchainStore<ChainBlockIndex> _chainBlockIndexes;

        public ChainManager(IBlockchainStore<Chain> chains,
            IBlockchainStore<ChainBlockLink> chainBlockLinks,
            IBlockchainStore<ChainBlockIndex> chainBlockIndexes)
        {
            _chains = chains;
            _chainBlockLinks = chainBlockLinks;
            _chainBlockIndexes = chainBlockIndexes;
        }

        public async Task<Chain> CreateAsync(int chainId, Hash genesisBlock)
        {
            var chain = await _chains.GetAsync(chainId.ToStorageKey());
            if (chain != null)
                throw new InvalidOperationException("chain already exists");

            chain = new Chain()
            {
                Id = chainId,
                LongestChainHeight = ChainConsts.GenesisBlockHeight,
                LongestChainHash = genesisBlock,
                BestChainHeight = ChainConsts.GenesisBlockHeight,
                BestChainHash = genesisBlock,
                GenesisBlockHash = genesisBlock,
                Branches =
                {
                    {genesisBlock.ToStorageKey(), ChainConsts.GenesisBlockHeight}
                }
            };

            await SetChainBlockLinkAsync(chainId, new ChainBlockLink()
            {
                BlockHash = genesisBlock,
                Height = ChainConsts.GenesisBlockHeight,
                PreviousBlockHash = Hash.Genesis,
                IsLinked = true
            });
            await _chains.SetAsync(chainId.ToStorageKey(), chain);

            return chain;
        }

        public async Task<Chain> GetAsync(int chainId)
        {
            var chain = await _chains.GetAsync(chainId.ToStorageKey());
            return chain;
        }

        public async Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, Hash blockHash)
        {
            return await GetChainBlockLinkAsync(chainId, blockHash.ToStorageKey());
        }

        protected async Task<ChainBlockLink> GetChainBlockLinkAsync(int chainId, string blockHash)
        {
            return await _chainBlockLinks.GetAsync(chainId.ToStorageKey() + blockHash);
        }

        public async Task SetChainBlockLinkAsync(int chainId, ChainBlockLink chainBlockLink)
        {
            await _chainBlockLinks.SetAsync(chainId.ToStorageKey() + chainBlockLink.BlockHash.ToStorageKey(),
                chainBlockLink);
        }

        private async Task SetChainBlockIndexAsync(int chainId, ulong blockHeight, Hash blockHash)
        {
            await _chainBlockIndexes.SetAsync(chainId.ToStorageKey() + blockHeight.ToStorageKey(),
                new ChainBlockIndex() {BlockHash = blockHash});
        }

        public async Task<ChainBlockIndex> GetChainBlockIndexAsync(int chainId, ulong blockHeight)
        {
            return await _chainBlockIndexes.GetAsync(chainId.ToStorageKey() + blockHeight.ToStorageKey());
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain,
            ChainBlockLink chainBlockLink)
        {
            BlockAttachOperationStatus status = BlockAttachOperationStatus.None;

            while (true)
            {
                var previousHash = chainBlockLink.PreviousBlockHash.ToStorageKey();
                var blockHash = chainBlockLink.BlockHash.ToStorageKey();

                if (chain.Branches.ContainsKey(previousHash))
                {
                    chain.Branches[blockHash] = chainBlockLink.Height;
                    chain.Branches.Remove(previousHash);

                    if (chainBlockLink.Height > chain.LongestChainHeight)
                    {
                        chain.LongestChainHeight = chainBlockLink.Height;
                        chain.LongestChainHash = chainBlockLink.BlockHash;
                        status |= BlockAttachOperationStatus.LongestChainFound;
                    }

                    if (chainBlockLink.IsLinked)
                        throw new Exception("chain block link should not be linked");

                    chainBlockLink.IsLinked = true;

                    await SetChainBlockLinkAsync(chain.Id, chainBlockLink);

                    if (!chain.NotLinkedBlocks.ContainsKey(blockHash))
                    {
                        status |= BlockAttachOperationStatus.NewBlockLinked;
                        break;
                    }

                    chainBlockLink = await GetChainBlockLinkAsync(chain.Id, chain.NotLinkedBlocks[blockHash]);

                    chain.NotLinkedBlocks.Remove(blockHash);

                    status |= BlockAttachOperationStatus.NewBlocksLinked;
                }
                else
                {
                    if (chainBlockLink.Height <= chain.LongestChainHeight)
                    {
                        //check database to ensure whether it can be a branch
                        var previousChainBlockLink =
                            await this.GetChainBlockLinkAsync(chain.Id, chainBlockLink.PreviousBlockHash);
                        if (previousChainBlockLink != null && previousChainBlockLink.IsLinked)
                        {
                            chain.Branches[previousChainBlockLink.BlockHash.ToStorageKey()] =
                                previousChainBlockLink.Height;
                            continue;
                        }
                    }

                    chain.NotLinkedBlocks[previousHash] = blockHash;

                    if (status != BlockAttachOperationStatus.None)
                        throw new Exception("invalid status");

                    status = BlockAttachOperationStatus.NewBlockNotLinked;
                    await SetChainBlockLinkAsync(chain.Id, chainBlockLink);
                    break;
                }
            }

            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

            return status;
        }

        public async Task<Hash> UpdateIrreversibleBlockAsync(Chain chain, Hash start, int confirmationsNeeded)
        {
            if (start == null)
                return null;
            
            HashSet<byte[]> foundKeys = new HashSet<byte[]>(new ByteArrayEqualityComparer());
            
            Hash newLib = null;
            Hash currentHash = start;
            
            while (true)
            {
                ChainBlockLink link = await GetChainBlockLinkAsync(chain.Id, start);

                if (link == null || link.IsIrreversibleBlock || !link.IsLinked)
                    return null;
                
                foundKeys.Add(link.Producer.ToByteArray());

                if (foundKeys.Count >= confirmationsNeeded)
                {
                    newLib = currentHash;
                    break;
                }

                currentHash = link.PreviousBlockHash;
            }

            if (newLib != null)
                await SetIrreversibleBlockAsync(chain, newLib);

            return newLib;
        }

        public async Task SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash)
        {
            Stack<ChainBlockLink> links = new Stack<ChainBlockLink>();

            while (true)
            {
                if (irreversibleBlockHash == null)
                    break;
                var chainBlockLink = await GetChainBlockLinkAsync(chain.Id, irreversibleBlockHash);
                if (chainBlockLink == null || chainBlockLink.IsIrreversibleBlock)
                    break;
                if (!chainBlockLink.IsLinked)
                    throw new InvalidOperationException("should not set an unlinked block as irreversible block");
                chainBlockLink.IsIrreversibleBlock = true;
                links.Push(chainBlockLink);
                irreversibleBlockHash = chainBlockLink.PreviousBlockHash;
            }

            while (links.Count > 0)
            {
                var chainBlockLink = links.Pop();
                await SetChainBlockIndexAsync(chain.Id, chainBlockLink.Height, chainBlockLink.BlockHash);
                await SetChainBlockLinkAsync(chain.Id, chainBlockLink);
                chain.LastIrreversibleBlockHash = chainBlockLink.BlockHash;
                chain.LastIrreversibleBlockHeight = chainBlockLink.Height;
                await _chains.SetAsync(chain.Id.ToStorageKey(), chain);
            }
        }

        public async Task<List<ChainBlockLink>> GetNotExecutedBlocks(int chainId, Hash blockHash)
        {
            var chain = await GetAsync(chainId);

            var output = new List<ChainBlockLink>();

            while (true)
            {
                var chainBlockLink = await GetChainBlockLinkAsync(chain.Id, blockHash);
                if (chainBlockLink != null)
                {
                    if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionNone)
                    {
                        output.Add(chainBlockLink);
                        if(chainBlockLink.PreviousBlockHash!=null)
                        blockHash = chainBlockLink.PreviousBlockHash;
                        continue;
                    }else if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionFailed)
                    {
                        output.Clear();
                    }
                }
                break;
            }
            
            output.Reverse();
            return output;
        }

        public async Task SetChainBlockLinkExecutionStatus(int chainId, ChainBlockLink blockLink,
            ChainBlockLinkExecutionStatus status)
        {
            if (blockLink.ExecutionStatus != ChainBlockLinkExecutionStatus.ExecutionNone ||
                status == ChainBlockLinkExecutionStatus.ExecutionNone)
                throw new InvalidOperationException();

            blockLink.ExecutionStatus = status;
            await SetChainBlockLinkAsync(chainId, blockLink);
        }

        public async Task SetBestChainAsync(Chain chain, ulong bestChainHeight, Hash bestChainHash)
        {
            if (chain.BestChainHeight > bestChainHeight)
            {
                throw new InvalidOperationException();
            }

            chain.BestChainHeight = bestChainHeight;
            chain.BestChainHash = bestChainHash;

            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);
        }
    }
}