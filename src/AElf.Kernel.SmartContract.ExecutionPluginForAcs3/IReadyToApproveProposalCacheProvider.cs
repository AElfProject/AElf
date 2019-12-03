using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs3
{
    public interface IReadyToApproveProposalCacheProvider
    {
        void CacheProposalToApprove(Hash proposalId, long height);
        List<Hash> GetCachedProposals();
        bool TryGetProposalCreatedHeight(Hash proposalId, out long height);
        void RemoveProposalById(Hash proposalId);
    }
}
