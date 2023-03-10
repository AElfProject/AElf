using System.Collections.Generic;
using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.CodeCheck.Application;

internal interface ICodeCheckReleasedProposalIdProvider
{
    Task AddProposalIdsAsync(BlockIndex blockIndex, List<Hash> proposalIds);
    Task<ProposalIdList> GetProposalIdsAsync(BlockIndex blockIndex);
    Task RemoveProposalIdAsync(BlockIndex blockIndex, Hash proposalId);
}

internal class CodeCheckReleasedProposalIdProvider : BlockExecutedDataBaseProvider<ProposalIdList>,
    ICodeCheckReleasedProposalIdProvider, ISingletonDependency
{
    public CodeCheckReleasedProposalIdProvider(
        ICachedBlockchainExecutedDataService<ProposalIdList> cachedBlockchainExecutedDataService) :
        base(cachedBlockchainExecutedDataService)
    {
        Logger = NullLogger<CodeCheckReleasedProposalIdProvider>.Instance;
    }

    public ILogger<CodeCheckReleasedProposalIdProvider> Logger { get; set; }

    public async Task AddProposalIdsAsync(BlockIndex blockIndex, List<Hash> proposalIds)
    {
        var proposalIdList = GetBlockExecutedData(blockIndex) ?? new ProposalIdList();
        proposalIdList.ProposalIds.AddRange(proposalIds);
        await AddBlockExecutedDataAsync(blockIndex, proposalIdList);
    }

    public async Task<ProposalIdList> GetProposalIdsAsync(BlockIndex blockIndex)
    {
        return GetBlockExecutedData(blockIndex)??new ProposalIdList();
    }

    public async Task RemoveProposalIdAsync(BlockIndex blockIndex, Hash proposalId)
    {
        var proposalIdList = GetBlockExecutedData(blockIndex);
        if (proposalIdList == null) return;

        if (proposalIdList.ProposalIds.Remove(proposalId))
        {
            await AddBlockExecutedDataAsync(blockIndex, proposalIdList);
        }
    }

    protected override string GetBlockExecutedDataName()
    {
        return "CodeCheckReleasedProposalId";
    }
}