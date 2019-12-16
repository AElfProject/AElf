using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    public class ReadyToApproveProposalCacheProvider : IReadyToApproveProposalCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Hash, long> _proposalsToApprove = new ConcurrentDictionary<Hash, long>();
        
        public void CacheProposalToApprove(Hash proposalId, long height)
        {
            // keep the higher block index 
            _proposalsToApprove.AddOrUpdate(proposalId, height, (hash, h) => h >= height ? h : height);
        }

        public List<Hash> GetCachedProposals()
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
