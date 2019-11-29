using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs3
{
    internal class ProposalService : IProposalService, ITransientDependency
    {
        private readonly IReadyToApproveProposalCacheProvider _readyToApproveProposalCacheProvider;
        private readonly IParliamentContractReaderFactory _parliamentContractReaderFactory;

        public ProposalService(IReadyToApproveProposalCacheProvider readyToApproveProposalCacheProvider, 
            IParliamentContractReaderFactory parliamentContractReaderFactory)
        {
            _readyToApproveProposalCacheProvider = readyToApproveProposalCacheProvider;
            _parliamentContractReaderFactory = parliamentContractReaderFactory;
        }

        public void AddNotApprovedProposal(Hash proposalId, long height)
        {
            _readyToApproveProposalCacheProvider.CacheProposalToApprove(proposalId, height);
        }

        public async Task<List<Hash>> GetNotApprovedProposalIdListAsync(Hash blockHash, long blockHeight)
        {
            var proposalIdList = _readyToApproveProposalCacheProvider.GetCachedProposals();
            var result = await _parliamentContractReaderFactory.Create(blockHash, blockHeight).FilterNotApprovedProposal.CallAsync(
                new ProposalIdList
                {
                    ProposalIds = {proposalIdList}
                });

            return result?.ProposalIds.ToList();
        }

        public async Task ClearProposalByLibAsync(Hash blockHash, long blockHeight)
        {
            var proposalIdList = _readyToApproveProposalCacheProvider.GetCachedProposals();
            var result = await _parliamentContractReaderFactory.Create(blockHash, blockHeight).FilterNotApprovedProposal.CallAsync(
                new ProposalIdList
                {
                    ProposalIds = {proposalIdList}
                });
            if (result == null)
                return;
            
            foreach (var proposalId in proposalIdList.Except(result.ProposalIds))
            {
                if (_readyToApproveProposalCacheProvider.TryGetProposalCreatedHeight(proposalId, out var h) &&
                    h <= blockHeight)
                {
                    _readyToApproveProposalCacheProvider.RemoveProposalById(proposalId);
                }
            }
        }
    }
}