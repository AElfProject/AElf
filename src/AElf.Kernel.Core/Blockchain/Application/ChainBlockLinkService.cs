using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.Blockchain.Application;

public interface IChainBlockLinkService
{
    List<ChainBlockLink> GetCachedChainBlockLinks();
    void CleanCachedChainBlockLinks(long height);
    Task<List<ChainBlockLink>> GetNotExecutedChainBlockLinksAsync(Hash chainBranchBlockHash);
    Task SetChainBlockLinkExecutionStatusAsync(Hash blockHash, ChainBlockLinkExecutionStatus status);
}

public class ChainBlockLinkService : IChainBlockLinkService, ITransientDependency
{
    private readonly IChainManager _chainManager;

    public ChainBlockLinkService(IChainManager chainManager)
    {
        _chainManager = chainManager;
    }

    public List<ChainBlockLink> GetCachedChainBlockLinks()
    {
        return _chainManager.GetCachedChainBlockLinks();
    }

    public void CleanCachedChainBlockLinks(long height)
    {
        _chainManager.CleanCachedChainBlockLinks(height);
    }

    public async Task<List<ChainBlockLink>> GetNotExecutedChainBlockLinksAsync(Hash chainBranchBlockHash)
    {
        return await _chainManager.GetNotExecutedBlocks(chainBranchBlockHash);
    }

    public async Task SetChainBlockLinkExecutionStatusAsync(Hash blockHash, ChainBlockLinkExecutionStatus status)
    {
        var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(blockHash);
        await _chainManager.SetChainBlockLinkExecutionStatusAsync(chainBlockLink, status);
    }
}