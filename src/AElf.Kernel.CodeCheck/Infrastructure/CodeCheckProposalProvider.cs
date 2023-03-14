using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.CodeCheck.Application;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public class CodeCheckProposalProvider : ICodeCheckProposalProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<Hash, CodeCheckProposal> _proposalsToRelease = new();

    public void AddProposal(Hash proposalId, Hash proposalInputHash, long height)
    {
        // keep the higher block index 
        _proposalsToRelease.AddOrUpdate(proposalId, new CodeCheckProposal
        {
            ProposalId = proposalId,
            ProposedContractInputHash = proposalInputHash,
            BlockHeight = height
        }, (hash, proposal) => proposal.BlockHeight >= height
            ? proposal
            : new CodeCheckProposal
            {
                ProposalId = proposalId,
                BlockHeight = height,
                ProposedContractInputHash = proposalInputHash
            });
    }

    public List<CodeCheckProposal> GetAllProposals()
    {
        return _proposalsToRelease.Values.ToList();
    }

    public bool TryGetProposalCreatedHeight(Hash proposalId, out long height)
    {
        if(_proposalsToRelease.TryGetValue(proposalId, out var proposal))
        {
            height = proposal.BlockHeight;
            return true;
        }

        height = 0;
        return false;
    }

    public void RemoveProposalById(Hash proposalId)
    {
        _proposalsToRelease.TryRemove(proposalId, out _);
    }
}