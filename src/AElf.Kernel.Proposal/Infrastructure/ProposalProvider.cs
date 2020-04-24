using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf.Kernel.Proposal.Infrastructure
{
    public class ProposalProvider : IProposalProvider
    {
        private readonly ConcurrentDictionary<Hash, long> _proposalsToApprove = new ConcurrentDictionary<Hash, long>();
        
        public void AddProposal(Hash proposalId, long height)
        {
            // keep the higher block index 
            _proposalsToApprove.AddOrUpdate(proposalId, height, (hash, h) => h >= height ? h : height);
        }

        public List<Hash> GetAllProposals()
        {
            return _proposalsToApprove.Keys.ToList();
        }

        public bool TryGetProposalCreatedHeight(Hash proposalId, out long height)
        {
            return _proposalsToApprove.TryGetValue(proposalId, out height);
        }

        public void RemoveProposalById(Hash proposalId)
        {
            _proposalsToApprove.TryRemove(proposalId, out _);
        }
    }
}
