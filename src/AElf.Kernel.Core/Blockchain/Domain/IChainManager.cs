using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
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
        Task<Chain> CreateAsync(Hash genesisBlock);
        Task<Chain> GetAsync();
        Task<ChainBlockLink> GetChainBlockLinkAsync(Hash blockHash);
        Task RemoveChainBlockLinkAsync(Hash blockHash);
        Task<ChainBlockIndex> GetChainBlockIndexAsync(long blockHeight);
        Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, ChainBlockLink chainBlockLink);
        Task<bool> SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash);
        Task<List<ChainBlockLink>> GetNotExecutedBlocks(Hash blockHash);
        Task SetChainBlockLinkExecutionStatus(ChainBlockLink blockLink, ChainBlockLinkExecutionStatus status);
        Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash);
        int GetChainId();
        Task<List<Hash>> CleanBranchesAsync(Chain chain, Hash irreversibleBlockHash, long irreversibleBlockHeight);
        Task RemoveLongestBranchAsync(Chain chain);
    }

    public class ChainManager : IChainManager, ISingletonDependency
    {
        private readonly IBlockchainStore<Chain> _chains;
        private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;
        private readonly IBlockchainStore<ChainBlockIndex> _chainBlockIndexes;

        private readonly IStaticChainInformationProvider _staticChainInformationProvider;

        private int ChainId => _staticChainInformationProvider.ChainId;

        public ILogger<ChainManager> Logger { get; set; }

        public ChainManager(IBlockchainStore<Chain> chains,
            IBlockchainStore<ChainBlockLink> chainBlockLinks,
            IBlockchainStore<ChainBlockIndex> chainBlockIndexes,
            IStaticChainInformationProvider staticChainInformationProvider)
        {
            _chains = chains;
            _chainBlockLinks = chainBlockLinks;
            _chainBlockIndexes = chainBlockIndexes;
            _staticChainInformationProvider = staticChainInformationProvider;
        }

        public async Task<Chain> CreateAsync(Hash genesisBlock)
        {
            var chain = await _chains.GetAsync(ChainId.ToStorageKey());
            if (chain != null)
                throw new InvalidOperationException("chain already exists");

            chain = new Chain()
            {
                Id = ChainId,
                LongestChainHeight = Constants.GenesisBlockHeight,
                LongestChainHash = genesisBlock,
                BestChainHeight = Constants.GenesisBlockHeight,
                BestChainHash = genesisBlock,
                GenesisBlockHash = genesisBlock,
                LastIrreversibleBlockHash = genesisBlock,
                LastIrreversibleBlockHeight = Constants.GenesisBlockHeight,
                Branches =
                {
                    {genesisBlock.ToStorageKey(), Constants.GenesisBlockHeight}
                }
            };

            await SetChainBlockLinkAsync(new ChainBlockLink()
            {
                BlockHash = genesisBlock,
                Height = Constants.GenesisBlockHeight,
                PreviousBlockHash = Hash.Empty,
                IsLinked = true,
                IsIrreversibleBlock = true
            });

            await SetChainBlockIndexAsync(Constants.GenesisBlockHeight, genesisBlock);

            await _chains.SetAsync(ChainId.ToStorageKey(), chain);

            return chain;
        }

        public async Task<Chain> GetAsync()
        {
            var chain = await _chains.GetAsync(ChainId.ToStorageKey());
            return chain;
        }

        public async Task<ChainBlockLink> GetChainBlockLinkAsync(Hash blockHash)
        {
            return await GetChainBlockLinkAsync(blockHash.ToStorageKey());
        }

        protected async Task<ChainBlockLink> GetChainBlockLinkAsync(string blockHash)
        {
            return await _chainBlockLinks.GetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + blockHash);
        }

        public async Task SetChainBlockLinkAsync(ChainBlockLink chainBlockLink)
        {
            await _chainBlockLinks.SetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + chainBlockLink.BlockHash.ToStorageKey(), chainBlockLink);
        }

        private async Task SetChainBlockIndexAsync(long blockHeight, Hash blockHash)
        {
            await _chainBlockIndexes.SetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + blockHeight.ToStorageKey(),
                new ChainBlockIndex() {BlockHash = blockHash});
        }

        public async Task RemoveChainBlockLinkAsync(Hash blockHash)
        {
            await _chainBlockLinks.RemoveAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator +  blockHash.ToStorageKey());
        }

        public async Task<ChainBlockIndex> GetChainBlockIndexAsync(long blockHeight)
        {
            return await _chainBlockIndexes.GetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + blockHeight.ToStorageKey());
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, ChainBlockLink chainBlockLink)
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

                    await SetChainBlockLinkAsync(chainBlockLink);

                    if (!chain.NotLinkedBlocks.ContainsKey(blockHash))
                    {
                        status |= BlockAttachOperationStatus.NewBlockLinked;
                        break;
                    }

                    chainBlockLink = await GetChainBlockLinkAsync(chain.NotLinkedBlocks[blockHash]);

                    chain.NotLinkedBlocks.Remove(blockHash);

                    status |= BlockAttachOperationStatus.NewBlocksLinked;
                }
                else
                {
                    if (chainBlockLink.Height <= chain.LongestChainHeight)
                    {
                        //check database to ensure whether it can be a branch
                        var previousChainBlockLink = await this.GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                        if (previousChainBlockLink != null && previousChainBlockLink.IsLinked)
                        {
                            chain.Branches[previousChainBlockLink.BlockHash.ToStorageKey()] = previousChainBlockLink.Height;
                            continue;
                        }
                    }

                    chain.NotLinkedBlocks[previousHash] = blockHash;

                    if (status != BlockAttachOperationStatus.None)
                        throw new Exception("invalid status");

                    status = BlockAttachOperationStatus.NewBlockNotLinked;
                    await SetChainBlockLinkAsync(chainBlockLink);
                    break;
                }
            }

            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

            Logger.LogInformation($"Attach {chainBlockLink.BlockHash} to longest chain, status: {status}, " +
                                  $"longest chain height: {chain.LongestChainHeight}, longest chain hash: {chain.LongestChainHash}");
            Logger.LogTrace($"Not linked blocks: {chain.NotLinkedBlocks}, branches: {chain.Branches}");

            return status;
        }

        public async Task<bool> SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash)
        {
            Stack<ChainBlockLink> links = new Stack<ChainBlockLink>();

            while (true)
            {
                if (irreversibleBlockHash == null)
                    break;
                var chainBlockLink = await GetChainBlockLinkAsync(irreversibleBlockHash);
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
                if (chainBlockLink.Height <= chain.LastIrreversibleBlockHeight) return false;
                await SetChainBlockIndexAsync(chainBlockLink.Height, chainBlockLink.BlockHash);
                await SetChainBlockLinkAsync(chainBlockLink);
                chain.LastIrreversibleBlockHash = chainBlockLink.BlockHash;
                chain.LastIrreversibleBlockHeight = chainBlockLink.Height;

                Logger.LogDebug($"Setting chain lib height: {chainBlockLink.Height}, chain lib hash: {chainBlockLink.BlockHash}");

                await _chains.SetAsync(chain.Id.ToStorageKey(), chain);
            }

            return true;
        }

        public async Task<List<ChainBlockLink>> GetNotExecutedBlocks(Hash blockHash)
        {
            var output = new List<ChainBlockLink>();
            while (true)
            {
                var chainBlockLink = await GetChainBlockLinkAsync(blockHash);
                if (chainBlockLink != null)
                {
                    if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionNone)
                    {
                        output.Add(chainBlockLink);
                        if (chainBlockLink.PreviousBlockHash != null)
                            blockHash = chainBlockLink.PreviousBlockHash;
                        continue;
                    }
                    else if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionFailed)
                    {
                        output.Clear();
                    }
                }

                break;
            }

            output.Reverse();
            return output;
        }

        public async Task SetChainBlockLinkExecutionStatus(ChainBlockLink blockLink,
            ChainBlockLinkExecutionStatus status)
        {
            if (blockLink.ExecutionStatus != ChainBlockLinkExecutionStatus.ExecutionNone ||
                status == ChainBlockLinkExecutionStatus.ExecutionNone)
                throw new InvalidOperationException();

            blockLink.ExecutionStatus = status;
            await SetChainBlockLinkAsync(blockLink);
        }

        public async Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash)
        {
            if (chain.BestChainHeight > bestChainHeight)
            {
                throw new InvalidOperationException();
            }

            chain.BestChainHeight = bestChainHeight;
            chain.BestChainHash = bestChainHash;

            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);
        }

        public int GetChainId()
        {
            return ChainId;
        }

        public async Task<List<Hash>> CleanBranchesAsync(Chain chain, Hash irreversibleBlockHash, long irreversibleBlockHeight)
        {
            var toRemoveBlocks = new List<Hash>();
            var toCleanBranchKeys = new List<string>();
            var toCleanNotLinkedKeys = new List<string>();

            var bestChainKey = chain.BestChainHash.ToStorageKey();

            foreach (var branch in chain.Branches)
            {
                if (branch.Key == bestChainKey)
                {
                    continue;
                }

                var toRemoveBlocksTemp = new List<Hash>();
                var chainBlockLink = await GetChainBlockLinkAsync(branch.Key);
                
                // Remove incorrect branch.
                // When an existing block is attached, will generate an incorrect branch.
                // and only clean up the branch, not clean the block in the branch.
                if (chainBlockLink != null)
                {
                    var chainBlockIndex = await GetChainBlockIndexAsync(chainBlockLink.Height);
                    if (chainBlockIndex != null && chainBlockIndex.BlockHash == chainBlockLink.BlockHash)
                    {
                        Logger.LogDebug($"Remove incorrect branch: {branch.Key}");
                        toCleanBranchKeys.Add(branch.Key);
                        continue;
                    }
                }

                while (true)
                {
                    if (chainBlockLink != null)
                    {

                        if (chainBlockLink.PreviousBlockHash == irreversibleBlockHash)
                        {
                            toRemoveBlocksTemp.Clear();
                            break;
                        }

                        // Use the height and hash alternatives to ChainBlockLink.IsIrreversibleBlock to verify,
                        // because ChainBlockLink can be overwrite 
                        if (chainBlockLink.Height < irreversibleBlockHeight)
                        {
                            var chainBlockIndex = await GetChainBlockIndexAsync(chainBlockLink.Height);
                            if (chainBlockIndex.BlockHash == chainBlockLink.BlockHash)
                            {
                                break;
                            }
                        }

                        toRemoveBlocksTemp.Add(chainBlockLink.BlockHash);
                        chainBlockLink = await GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                    }
                    else
                    {
                        toCleanBranchKeys.Add(branch.Key);
                        break;
                    }
                }

                if (toRemoveBlocksTemp.Count > 0)
                {
                    toRemoveBlocks.AddRange(toRemoveBlocksTemp);
                    toCleanBranchKeys.Add(branch.Key);
                }
            }

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var blockLink = await GetChainBlockLinkAsync(notLinkedBlock.Value);
                if (blockLink == null)
                {
                    toCleanNotLinkedKeys.Add(notLinkedBlock.Key);
                    continue;
                }

                if (blockLink.Height <= irreversibleBlockHeight)
                {
                    toRemoveBlocks.Add(blockLink.BlockHash);
                    toCleanNotLinkedKeys.Add(notLinkedBlock.Key);
                }
            }

            Logger.LogTrace($"Cleanup branches: [{toCleanBranchKeys.JoinAsString(",")}]");
            Logger.LogTrace($"Cleanup blocks: [{toRemoveBlocks.JoinAsString(",")}]");

            await RemoveChainBranchesAsync(chain, toCleanBranchKeys, toCleanNotLinkedKeys);

            return toRemoveBlocks;
        }

        private async Task RemoveChainBranchesAsync(Chain chain, List<string> branchKeys, List<string> notLinkedKeys)
        {
            var longestChainKey = chain.LongestChainHash.ToStorageKey();
            foreach (var key in branchKeys)
            {
                if (key == longestChainKey)
                {
                    chain.LongestChainHash = chain.BestChainHash;
                    chain.LongestChainHeight = chain.BestChainHeight;
                }

                chain.Branches.Remove(key);
            }

            foreach (var key in notLinkedKeys)
            {
                chain.NotLinkedBlocks.Remove(key);
            }

            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);
        }

        public async Task RemoveLongestBranchAsync(Chain chain)
        {
            chain.Branches.Remove(chain.LongestChainHash.ToStorageKey());
            chain.Branches[chain.BestChainHash.ToStorageKey()] = chain.BestChainHeight;

            chain.LongestChainHash = chain.BestChainHash;
            chain.LongestChainHeight = chain.BestChainHeight;
            Logger.LogWarning(
                $"Switch Longest chain to height: {chain.LongestChainHeight}, hash: {chain.LongestChainHash}.");

            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);
        }
    }
}