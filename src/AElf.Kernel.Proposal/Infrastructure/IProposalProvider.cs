using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.Proposal.Infrastructure
{
    public interface IProposalProvider
    {
        void AddProposal(Hash proposalId, long height);
        List<Hash> GetAllProposals();
        bool TryGetProposalCreatedHeight(Hash proposalId, out long height);
        void RemoveProposalById(Hash proposalId);
    }
}
