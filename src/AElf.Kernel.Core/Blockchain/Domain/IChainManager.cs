using System;
using System.Linq;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace AElf.Kernel.Blockchain.Domain;

[Flags]
public enum BlockAttachOperationStatus
{
    None = 0,
    NewBlockNotLinked = 1 << 1,
    NewBlockLinked = 1 << 2,
    LongestChainFound = (1 << 3) | NewBlockLinked,
    NewBlocksLinked = (1 << 4) | NewBlockLinked
}

public interface IChainManager
{
    Task<Chain> CreateAsync(Hash genesisBlock);
    Task<Chain> GetAsync();
    Task<ChainBlockLink> GetChainBlockLinkAsync(Hash blockHash);
    List<ChainBlockLink> GetCachedChainBlockLinks();
    Task RemoveChainBlockLinkAsync(Hash blockHash);
    void CleanCachedChainBlockLinks(long height);
    Task<ChainBlockIndex> GetChainBlockIndexAsync(long blockHeight);
    Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, ChainBlockLink chainBlockLink);
    Task<bool> SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash);
    Task<List<ChainBlockLink>> GetNotExecutedBlocks(Hash blockHash);
    Task SetChainBlockLinkExecutionStatusAsync(ChainBlockLink blockLink, ChainBlockLinkExecutionStatus status);

    Task SetChainBlockLinkExecutionStatusesAsync(IList<ChainBlockLink> blockLinks,
        ChainBlockLinkExecutionStatus status);

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
    private readonly IBlockchainStore<ChainBlockIndex> _chainBlockIndexes;
    private readonly IChainBlockLinkCacheProvider _chainBlockLinkCacheProvider;
    private readonly IBlockchainStore<ChainBlockLink> _chainBlockLinks;
    private readonly IBlockchainStore<Chain> _chains;
    private readonly Dictionary<int, Chain> _chainCache;

    private readonly IStaticChainInformationProvider _staticChainInformationProvider;

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
        _chainCache = new Dictionary<int, Chain>();
    }

    private int ChainId => _staticChainInformationProvider.ChainId;

    public ILogger<ChainManager> Logger { get; set; }

    public async Task<Chain> CreateAsync(Hash genesisBlock)
    {
        var chain = await _chains.GetAsync(ChainId.ToStorageKey());
        if (chain != null)
            throw new InvalidOperationException("chain already exists");

        chain = new Chain
        {
            Id = ChainId,
            LongestChainHeight = AElfConstants.GenesisBlockHeight,
            LongestChainHash = genesisBlock,
            BestChainHeight = AElfConstants.GenesisBlockHeight,
            BestChainHash = genesisBlock,
            GenesisBlockHash = genesisBlock,
            LastIrreversibleBlockHash = genesisBlock,
            LastIrreversibleBlockHeight = AElfConstants.GenesisBlockHeight,
            Branches =
            {
                { genesisBlock.ToStorageKey(), AElfConstants.GenesisBlockHeight }
            }
        };

        await SetChainBlockLinkAsync(new ChainBlockLink
        {
            BlockHash = genesisBlock,
            Height = AElfConstants.GenesisBlockHeight,
            PreviousBlockHash = Hash.Empty,
            IsLinked = true,
            IsIrreversibleBlock = true
        });

        await SetChainBlockIndexAsync(AElfConstants.GenesisBlockHeight, genesisBlock);

        await _chains.SetAsync(ChainId.ToStorageKey(), chain);

        // Update the cache.
        _chainCache[ChainId] = chain;
        return chain;
    }

    public async Task<Chain> GetAsync()
    {
        if (_chainCache.TryGetValue(ChainId, out var chain))
        {
            return chain?.Clone();
        }
        chain = await _chains.GetAsync(ChainId.ToStorageKey());
        _chainCache[ChainId] = chain;
        return chain;
    }

    public async Task<ChainBlockLink> GetChainBlockLinkAsync(Hash blockHash)
    {
        var chainBlockLink = _chainBlockLinkCacheProvider.GetChainBlockLink(blockHash);
        if (chainBlockLink != null) return chainBlockLink;
        return await GetChainBlockLinkAsync(blockHash.ToStorageKey());
    }

    public List<ChainBlockLink> GetCachedChainBlockLinks()
    {
        return _chainBlockLinkCacheProvider.GetChainBlockLinks();
    }

    public async Task RemoveChainBlockLinkAsync(Hash blockHash)
    {
        await _chainBlockLinks.RemoveAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator +
                                           blockHash.ToStorageKey());
        _chainBlockLinkCacheProvider.RemoveChainBlockLink(blockHash);
    }

    public void CleanCachedChainBlockLinks(long height)
    {
        var chainBlockLinks = _chainBlockLinkCacheProvider.GetChainBlockLinks()
            .Where(b => b.Height <= height)
            .OrderBy(b => b.Height).ToList();
        foreach (var chainBlockLink in chainBlockLinks)
            _chainBlockLinkCacheProvider.RemoveChainBlockLink(chainBlockLink.BlockHash);
    }

    public async Task<ChainBlockIndex> GetChainBlockIndexAsync(long blockHeight)
    {
        return await _chainBlockIndexes.GetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator +
                                                 blockHeight.ToStorageKey());
    }

    public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, ChainBlockLink chainBlockLink)
    {
        var status = BlockAttachOperationStatus.None;

        var isLinkedToLongestChain = chainBlockLink.PreviousBlockHash == chain.LongestChainHash &&
                                     chainBlockLink.Height == chain.LongestChainHeight + 1;

        Logger.LogDebug(
            $"Start attach block hash {chainBlockLink.BlockHash}, height {chainBlockLink.Height}");

        while (true)
        {
            var previousHash = chainBlockLink.PreviousBlockHash.ToStorageKey();
            var blockHash = chainBlockLink.BlockHash.ToStorageKey();

            if (chain.Branches.ContainsKey(previousHash))
            {
                chain.Branches[blockHash] = chainBlockLink.Height;
                chain.Branches.Remove(previousHash);

                if ((isLinkedToLongestChain && chainBlockLink.Height > chain.LongestChainHeight)
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
                var previousChainBlockLink = await GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
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

        // Update the cache.
        _chainCache[ChainId] = chain;

        return status;
    }

    public async Task<bool> SetIrreversibleBlockAsync(Chain chain, Hash irreversibleBlockHash)
    {
        var links = new List<ChainBlockLink>();

        while (true)
        {
            var chainBlockLink = await GetChainBlockLinkAsync(irreversibleBlockHash);
            if (chainBlockLink == null || !chainBlockLink.IsLinked)
                throw new InvalidOperationException(
                    $"should not set an unlinked block as irreversible block, height: {chainBlockLink?.Height}, hash: {chainBlockLink?.BlockHash}");
            if (chainBlockLink.IsIrreversibleBlock)
                break;
            chainBlockLink.IsIrreversibleBlock = true;
            links.Add(chainBlockLink);
            irreversibleBlockHash = chainBlockLink.PreviousBlockHash;
        }

        if (links.Count > 0)
        {
            if (links.Last().Height <= chain.LastIrreversibleBlockHeight)
                return false;
            await SetChainBlockIndexesAsync(links.ToDictionary(l => l.Height, l => l.BlockHash));
            await SetChainBlockLinksAsync(links);
            chain.LastIrreversibleBlockHash = links.First().BlockHash;
            chain.LastIrreversibleBlockHeight = links.First().Height;
            await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

            // Update the cache.
            _chainCache[ChainId] = chain;

            Logger.LogDebug(
                $"Setting chain lib height: {chain.LastIrreversibleBlockHeight}, chain lib hash: {chain.LastIrreversibleBlockHash}");

            return true;
        }

        return false;
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

                if (chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionFailed)
                {
                    output.Clear();
                }
            }

            break;
        }

        output.Reverse();
        return output;
    }

    public async Task SetChainBlockLinkExecutionStatusAsync(ChainBlockLink blockLink,
        ChainBlockLinkExecutionStatus status)
    {
        if (blockLink.ExecutionStatus != ChainBlockLinkExecutionStatus.ExecutionNone ||
            status == ChainBlockLinkExecutionStatus.ExecutionNone)
            throw new InvalidOperationException();

        blockLink.ExecutionStatus = status;
        await SetChainBlockLinkAsync(blockLink);
    }

    public async Task SetChainBlockLinkExecutionStatusesAsync(IList<ChainBlockLink> blockLinks,
        ChainBlockLinkExecutionStatus status)
    {
        foreach (var blockLink in blockLinks)
        {
            if (blockLink.ExecutionStatus != ChainBlockLinkExecutionStatus.ExecutionNone ||
                status == ChainBlockLinkExecutionStatus.ExecutionNone)
                throw new InvalidOperationException();

            blockLink.ExecutionStatus = status;
        }

        await SetChainBlockLinksAsync(blockLinks);
    }

    public async Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash)
    {
        if (chain.BestChainHeight > bestChainHeight) throw new InvalidOperationException();

        chain.BestChainHeight = bestChainHeight;
        chain.BestChainHash = bestChainHash;

        await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

        // Update the cache.
        _chainCache[ChainId] = chain;
    }

    public int GetChainId()
    {
        return ChainId;
    }

    public async Task<DiscardedBranch> GetDiscardedBranchAsync(Chain chain, Hash irreversibleBlockHash,
        long irreversibleBlockHeight)
    {
        var toCleanBranchKeys = new List<string>();

        var bestChainKey = chain.BestChainHash.ToStorageKey();

        foreach (var branch in chain.Branches)
        {
            if (branch.Key == bestChainKey) continue;

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

                if (chainBlockLink.PreviousBlockHash == irreversibleBlockHash) break;

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

            if (isDiscardedBranch) toCleanBranchKeys.Add(branch.Key);
        }

        var toCleanNotLinkedKeys = await GetNotLinkedKeysAsync(chain, irreversibleBlockHeight);

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
            if (key == bestChainKey) continue;

            if (key == longestChainKey)
            {
                chain.LongestChainHash = chain.BestChainHash;
                chain.LongestChainHeight = chain.BestChainHeight;
            }

            chain.Branches.Remove(key);
        }

        foreach (var key in discardedBranch.NotLinkedKeys) chain.NotLinkedBlocks.Remove(key);

        Logger.LogDebug(
            $"Clean chain branch, Branches: [{discardedBranch.BranchKeys.JoinAsString(",")}], NotLinkedBlocks: [{discardedBranch.NotLinkedKeys.JoinAsString(",")}]");

        await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

        // Update the cache.
        _chainCache[ChainId] = chain;
    }

    public async Task RemoveLongestBranchAsync(Chain chain)
    {
        chain.Branches.Remove(chain.LongestChainHash.ToStorageKey());
        chain.Branches[chain.BestChainHash.ToStorageKey()] = chain.BestChainHeight;

        chain.LongestChainHash = chain.BestChainHash;
        chain.LongestChainHeight = chain.BestChainHeight;
        Logger.LogInformation(
            $"Switch Longest chain to height: {chain.LongestChainHeight}, hash: {chain.LongestChainHash}.");

        await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

        // Update the cache.
        _chainCache[ChainId] = chain;
    }

    public async Task<Chain> ResetChainToLibAsync(Chain chain)
    {
        var libHash = chain.LastIrreversibleBlockHash;
        var libHeight = chain.LastIrreversibleBlockHeight;

        foreach (var branch in chain.Branches)
        {
            var hash = Hash.LoadFromBase64(branch.Key);
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

        Logger.LogInformation($"Rollback to height {chain.BestChainHeight}, hash {chain.BestChainHash}.");
        await _chains.SetAsync(chain.Id.ToStorageKey(), chain);

        // Update the cache.
        _chainCache[ChainId] = chain;

        return chain;
    }

    private async Task<ChainBlockLink> GetChainBlockLinkWithCacheAsync(string blockHash)
    {
        var hash = new Hash { Value = ByteString.FromBase64(blockHash) };
        var chainBlockLink = _chainBlockLinkCacheProvider.GetChainBlockLink(hash);
        if (chainBlockLink != null) return chainBlockLink;
        return await GetChainBlockLinkAsync(blockHash);
    }

    private async Task<ChainBlockLink> GetChainBlockLinkAsync(string blockHash)
    {
        return await _chainBlockLinks.GetAsync(ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator +
                                               blockHash);
    }

    private async Task<List<ChainBlockLink>> GetChainBlockLinksAsync(IList<string> blockHashes)
    {
        var prefix = ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator;
        return await _chainBlockLinks.GetAllAsync(blockHashes.Select(h => prefix + h).ToList());
    }

    public async Task SetChainBlockLinkAsync(ChainBlockLink chainBlockLink)
    {
        await _chainBlockLinks.SetAsync(
            ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + chainBlockLink.BlockHash.ToStorageKey(),
            chainBlockLink);
        _chainBlockLinkCacheProvider.SetChainBlockLink(chainBlockLink);
    }

    private async Task SetChainBlockLinksAsync(IList<ChainBlockLink> chainBlockLinks)
    {
        var prefix = ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator;
        await _chainBlockLinks.SetAllAsync(chainBlockLinks.ToDictionary(l => prefix + l.BlockHash.ToStorageKey(),
            l => l));
        foreach (var chainBlockLink in chainBlockLinks) _chainBlockLinkCacheProvider.SetChainBlockLink(chainBlockLink);
    }

    private async Task SetChainBlockIndexAsync(long blockHeight, Hash blockHash)
    {
        await _chainBlockIndexes.SetAsync(
            ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator + blockHeight.ToStorageKey(),
            new ChainBlockIndex { BlockHash = blockHash });
    }

    private async Task SetChainBlockIndexesAsync(IDictionary<long, Hash> blockIndexes)
    {
        var prefix = ChainId.ToStorageKey() + KernelConstants.StorageKeySeparator;
        await _chainBlockIndexes.SetAllAsync(blockIndexes.ToDictionary(d => prefix + d.Key.ToStorageKey(),
            d => new ChainBlockIndex { BlockHash = d.Value }));
    }

    private async Task<List<string>> GetNotLinkedKeysAsync(Chain chain, long irreversibleBlockHeight)
    {
        var toCleanNotLinkedKeys = new List<string>();
        foreach (var notLinkedBlock in chain.NotLinkedBlocks)
        {
            var blockLink = await GetChainBlockLinkWithCacheAsync(notLinkedBlock.Value);
            if (blockLink == null)
            {
                toCleanNotLinkedKeys.Add(notLinkedBlock.Key);
                continue;
            }

            if (blockLink.Height <= irreversibleBlockHeight) toCleanNotLinkedKeys.Add(notLinkedBlock.Key);
        }

        return toCleanNotLinkedKeys;
    }
}