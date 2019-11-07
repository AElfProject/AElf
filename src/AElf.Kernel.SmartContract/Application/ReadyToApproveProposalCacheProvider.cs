using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IReadyToApproveProposalCacheProvider
    {
        void TryCacheProposalToApprove(Hash proposalId);
        bool TryGetProposalToApprove(out Hash proposalId);
    }

    public class ReadyToApproveProposalCacheProvider : IReadyToApproveProposalCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentBag<Hash> _proposalsToApprove = new ConcurrentBag<Hash>();
        
        public void TryCacheProposalToApprove(Hash proposalId)
        {
            _proposalsToApprove.Add(proposalId);
        }

        public bool TryGetProposalToApprove(out Hash proposalId)
        {
            return _proposalsToApprove.TryTake(out proposalId);
        }
    }
}
