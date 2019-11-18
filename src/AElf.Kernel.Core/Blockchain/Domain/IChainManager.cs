using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Google.Protobuf;
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
        ChainBlockLink GetCachedChainBlockLink(Hash blockHash);
        List<ChainBlockLink> GetCachedChainBlockLinks();
        Task RemoveChainBlockLinkAsync(Hash blockHash);
        void RemoveCachedChainBlockLink(Hash blockHash);
        Task<ChainBlockIndex> GetChainBlockIndexAsync(long blockHeight);
        Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, ChainBlockLink chainBlockLink);
        Task<bool> SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash);
        Task<List<ChainBlockLink>> GetNotExecutedBlocks(Hash blockHash);
        Task SetChainBlockLinkExecutionStatus(ChainBlockLink blockLink, ChainBlockLinkExecutionStatus status);
        Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash);
        int GetChainId();
        Task RemoveLongestBranchAsync(Chain chain);
        Task<DiscardedBranch> GetDiscardedBranchAsync(Chain chain, Hash irreversibleBlockHash,
            long irreversibleBlockHeight);
        Task CleanChainBranchAsync(Chain chain, DiscardedBranch discardedBranch);
        Task<Chain> ResetChainToLibAsync(Chain chain);
    }

    public class ChainManager : IChainManager, ISingletonDependency
    {
        private readonly IBlockchainStore<Chain> _chains;
        private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;
        private readonly IBlockchainStore<ChainBlockIndex> _chainBlockIndexes;

        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly IChainBlockLinkCacheProvider _chainBlockLinkCacheProvider;

        private int ChainId => _staticChainInformationProvider.ChainId;

        public ILogger<ChainManager> Logger { get; set; }

        public ChainManager(IBlockchainStore<Chain> chains,
            IBlockchainStore<ChainBlockLink> chainBlockLinks,
            IBlockchainStore<ChainBlockIndex> chainBlockIndexes,
            IStaticChainInformationProvider staticChainInformationProvider, 
            IChainBlockLinkCacheProvider chainBlockLinkCacheProvider)
        {
            _chains = chains;
            _chainBlockLinks = chainBlockLinks;
            _chainBlockIndexes = chainBlockIndexes;
            _staticChainInformationProvider = staticChainInformationProvider;
            _chainBlockLinkCacheProvider = chainBlockLinkCacheProvider;
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
            var chainBlockLink = _chainBlockLinkCacheProvider.GetChainBlockLink(blockHash);
            if (chainBlockLink != null) return chainBlockLink;
            return await GetChainBlockLinkAsync(blockHash.ToStorageKey());
        }

        public ChainBlockLink GetCachedChainBlockLink(Hash blockHash)
        {
            return _chainBlockLinkCacheProvider.GetChainBlockLink(blockHash);
        }

        public List<ChainBlockLink> GetCachedChainBlockLinks()
        {
            return _chainBlockLinkCacheProvider.GetChainBlockLinks();
        }

        protected async Task<ChainBlockLink> GetChainBlockLinkWithCacheAsync(string blockHash)
        {
            var hash = new Hash {Value = ByteString.FromBase64(blockHash)};
            var chainBlockLink = _chainBlockLinkCacheProvider.GetChainBlockLink(hash);
            if (chainBlockLink != null) return chainBlockLink;
            return await GetChainBlockLinkAsync(blockHash);
        }

        protected async Task<ChainBlockLink> GetChainBlockLinkAsync(string blockHash)
        {
            return await _chainBlockLinks.GetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + blockHash);
        }

        public async Task SetChainBlockLinkAsync(ChainBlockLink chainBlockLink)
        {
            _chainBlockLinkCacheProvider.SetChainBlockLink(chainBlockLink);
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
        
        public void RemoveCachedChainBlockLink(Hash blockHash)
        {
            _chainBlockLinkCacheProvider.RemoveChainBlockLink(blockHash);
        }

        public async Task<ChainBlockIndex> GetChainBlockIndexAsync(long blockHeight)
        {
            return await _chainBlockIndexes.GetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + blockHeight.ToStorageKey());
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, ChainBlockLink chainBlockLink)
        {
            BlockAttachOperationStatus status = BlockAttachOperationStatus.None;

            bool isLinkedToLongestChain = chainBlockLink.PreviousBlockHash == chain.LongestChainHash &&
                                          chainBlockLink.Height == chain.LongestChainHeight + 1;

            Logger.LogTrace($"Start attach block hash {chainBlockLink.BlockHash}, height {chainBlockLink.Height}");
            
            while (true)
            {
                var previousHash = chainBlockLink.PreviousBlockHash.ToStorageKey();
                var blockHash = chainBlockLink.BlockHash.ToStorageKey();

                if (chain.Branches.ContainsKey(previousHash))
                {
                    chain.Branches[blockHash] = chainBlockLink.Height;
                    chain.Branches.Remove(previousHash);

                    //TODO: change the longest chain switch length 
                    if (isLinkedToLongestChain && chainBlockLink.Height > chain.LongestChainHeight
                        || chainBlockLink.Height >= chain.LongestChainHeight + 8)
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

                    chainBlockLink = await GetChainBlockLinkWithCacheAsync(chain.NotLinkedBlocks[blockHash]);

                    chain.NotLinkedBlocks.Remove(blockHash);

                    status |= BlockAttachOperationStatus.NewBlocksLinked;
                }
                else
                {
                    //check database to ensure whether it can be a branch
                    var previousChainBlockLink = await this.GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                    if (previousChainBlockLink != null && previousChainBlockLink.IsLinked)
                    {
                        chain.Branches[previousChainBlockLink.BlockHash.ToStorageKey()] = previousChainBlockLink.Height;
                        continue;
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
//            Logger.LogTrace($"Not linked blocks: {chain.NotLinkedBlocks}, branches: {chain.Branches}");

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
                    throw new InvalidOperationException($"should not set an unlinked block as irreversible block, height: {chainBlockLink.Height}, hash: {chainBlockLink.BlockHash}");
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

        public async Task<DiscardedBranch> GetDiscardedBranchAsync(Chain chain, Hash irreversibleBlockHash, long irreversibleBlockHeight)
        {
            var toCleanBranchKeys = new List<string>();
            var toCleanNotLinkedKeys = new List<string>();

            var bestChainKey = chain.BestChainHash.ToStorageKey();

            foreach (var branch in chain.Branches)
            {
                if (branch.Key == bestChainKey)
                {
                    continue;
                }

                var chainBlockLink = await GetChainBlockLinkWithCacheAsync(branch.Key);
                
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

                var isDiscardedBranch = false;
                while (true)
                {
                    if (chainBlockLink == null)
                    {
                        isDiscardedBranch = true;
                        break;
                    }

                    if (chainBlockLink.PreviousBlockHash == irreversibleBlockHash)
                    {
                        break;
                    }

                    // Use the height and hash alternatives to ChainBlockLink.IsIrreversibleBlock to verify,
                    // because ChainBlockLink can be overwrite 
                    if (chainBlockLink.Height < irreversibleBlockHeight)
                    {
                        var chainBlockIndex = await GetChainBlockIndexAsync(chainBlockLink.Height);
                        if (chainBlockIndex.BlockHash == chainBlockLink.BlockHash)
                        {
                            isDiscardedBranch = true;
                            break;
                        }
                    }

                    chainBlockLink = await GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                }

                if (isDiscardedBranch)
                {
                    toCleanBranchKeys.Add(branch.Key);
                }
            }

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var blockLink = await GetChainBlockLinkWithCacheAsync(notLinkedBlock.Value);
                if (blockLink == null)
                {
                    toCleanNotLinkedKeys.Add(notLinkedBlock.Key);
                    continue;
                }

                if (blockLink.Height <= irreversibleBlockHeight)
                {
                    toCleanNotLinkedKeys.Add(notLinkedBlock.Key);
                }
            }
            
            return new DiscardedBranch
            {
                BranchKeys = toCleanBranchKeys,
                NotLinkedKeys = toCleanNotLinkedKeys
            };
        }

        public async Task CleanChainBranchAsync(Chain chain, DiscardedBranch discardedBranch)
        {
            var longestChainKey = chain.LongestChainHash.ToStorageKey();
            var bestChainKey = chain.BestChainHash.ToStorageKey();
            foreach (var key in discardedBranch.BranchKeys)
            {
                if (key == bestChainKey)
                {
                    continue;
                }

                if (key == longestChainKey)
                {
                    chain.LongestChainHash = chain.BestChainHash;
                    chain.LongestChainHeight = chain.BestChainHeight;
                }

                chain.Branches.Remove(key);
            }

            foreach (var key in discardedBranch.NotLinkedKeys)
            {
                chain.NotLinkedBlocks.Remove(key);
            }

            Logger.LogDebug(
                $"Clean chain branch, Branches: [{discardedBranch.BranchKeys.JoinAsString(",")}], NotLinkedBlocks: [{discardedBranch.NotLinkedKeys.JoinAsString(",")}]");

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

        public async Task<Chain> ResetChainToLibAsync(Chain chain)
        {
            var libHash = chain.LastIrreversibleBlockHash;
            var libHeight = chain.LastIrreversibleBlockHeight;

            foreach (var branch in chain.Branches)
            {
                var hash = HashHelper.Base64ToHash(branch.Key);
                var chainBlockLink = await GetChainBlockLinkAsync(hash);

                while (chainBlockLink != null && chainBlockLink.Height > libHeight)
                {
                    chainBlockLink.ExecutionStatus = ChainBlockLinkExecutionStatus.ExecutionNone;
                    chainBlockLink.IsLinked = false;
                    await SetChainBlockLinkAsync(chainBlockLink);
                    
                    chainBlockLink = await GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                }
            }
            
            chain.Branches.Clear();
            chain.NotLinkedBlocks.Clear();
            
            chain.Branches[libHash.ToStorageKey()] = libHeight;

            chain.BestChainHash = libHash;
            chain.BestChainHeight = libHeight;
            chain.LongestChainHash = libHash;
            chain.LongestChainHeight = libHeight;

            Logger.LogTrace($"Rollback to height {chain.BestChainHeight}, hash {chain.BestChainHash}.");
            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

            return chain;
        }
    }
}