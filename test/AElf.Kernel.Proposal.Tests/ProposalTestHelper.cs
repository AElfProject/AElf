using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Parliament;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Proposal.Tests
{
    public class ProposalTestHelper : ISingletonDependency
    {
        private readonly List<Hash> _notApprovedProposalIdList = new List<Hash>();
        private readonly List<Hash> _notApprovedPendingProposalIdList = new List<Hash>();

        internal void AddNotVotedProposalIdList(List<Hash> proposalIdList)
        {
            _notApprovedProposalIdList.AddRange(proposalIdList);
        }

        internal void AddNotVotedPendingProposalIdList(List<Hash> proposalIdList)
        {
            _notApprovedPendingProposalIdList.AddRange(proposalIdList);
        }

        internal ProposalIdList GetNotVotedProposalIdList(ProposalIdList proposalIdList)
        {
            return new ProposalIdList
            {
                ProposalIds =
                    {proposalIdList.ProposalIds.Where(proposalId => _notApprovedProposalIdList.Contains(proposalId))}
            };
        }

        internal ProposalIdList GetNotVotedPendingProposalIdList(ProposalIdList proposalIdList)
        {
            return new ProposalIdList
            {
                ProposalIds =
                {
                    proposalIdList.ProposalIds.Where(proposalId =>
                        _notApprovedPendingProposalIdList.Contains(proposalId))
                }
            };
        }
    }
}