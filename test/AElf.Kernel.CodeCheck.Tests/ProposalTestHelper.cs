using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Parliament;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.CodeCheck;

public class ProposalTestHelper : ISingletonDependency
{
    private readonly List<Hash> _releaseThresholdReachedProposalIdList = new();
    private readonly List<Hash> _availableProposalIdList = new();

    internal void AddReleaseThresholdReachedProposalIdList(List<Hash> proposalIdList)
    {
        _releaseThresholdReachedProposalIdList.AddRange(proposalIdList);
    }

    internal void AddAvailableProposalIdList(List<Hash> proposalIdList)
    {
        _availableProposalIdList.AddRange(proposalIdList);
    }

    internal ProposalIdList GetReleaseThresholdReachedProposals(ProposalIdList proposalIdList)
    {
        return new ProposalIdList
        {
            ProposalIds =
                { proposalIdList.ProposalIds.Where(proposalId => _releaseThresholdReachedProposalIdList.Contains(proposalId)) }
        };
    }

    internal ProposalIdList GetAvailableProposals(ProposalIdList proposalIdList)
    {
        return new ProposalIdList
        {
            ProposalIds =
            {
                proposalIdList.ProposalIds.Where(proposalId =>
                    _availableProposalIdList.Contains(proposalId))
            }
        };
    }
}