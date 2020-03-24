using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Proposal.Application
{
    internal class ProposalService : IProposalService, ITransientDependency
    {
        private readonly IProposalProvider _proposalProvider;
        private readonly IParliamentContractReaderFactory _parliamentContractReaderFactory;

        public ILogger<ProposalService> Logger { get; set; }

        public ProposalService(IProposalProvider proposalProvider,
            IParliamentContractReaderFactory parliamentContractReaderFactory)
        {
            _proposalProvider = proposalProvider;
            _parliamentContractReaderFactory = parliamentContractReaderFactory;
        }

        public void AddNotApprovedProposal(Hash proposalId, long height)
        {
            _proposalProvider.AddProposal(proposalId, height);
        }

        public async Task<List<Hash>> GetNotApprovedProposalIdListAsync(Address @from, Hash blockHash, long blockHeight)
        {
            var proposalIdList = _proposalProvider.GetAllProposals();
            var result = await _parliamentContractReaderFactory.Create(blockHash, blockHeight, from)
                .GetNotVotedProposals.CallAsync(new ProposalIdList {ProposalIds = {proposalIdList}});

            return result?.ProposalIds.ToList();
        }

        public async Task ClearProposalByLibAsync(Hash blockHash, long blockHeight)
        {
            var proposalIdList = _proposalProvider.GetAllProposals();
            var result = await _parliamentContractReaderFactory.Create(blockHash, blockHeight).GetNotVotedPendingProposals
                .CallAsync(new ProposalIdList {ProposalIds = {proposalIdList}});
            if (result == null)
                return;

            foreach (var proposalId in proposalIdList.Except(result.ProposalIds))
            {
                if (!_proposalProvider.TryGetProposalCreatedHeight(proposalId, out var h) ||
                    h > blockHeight)
                    continue;
                Logger.LogTrace($"Clear proposal {proposalId} by LIB hash {blockHash}, height {blockHeight}");
                _proposalProvider.RemoveProposalById(proposalId);
            }
        }
    }
}