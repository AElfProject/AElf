using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    public interface IProposalService
    {
        void AddNotApprovedProposal(Hash proposalId, long height);
        Task<List<Hash>> GetNotApprovedProposalIdListAsync(Hash blockHash, long blockHeight);
        Task ClearProposalByLibAsync(Hash blockHash, long blockHeight);
    }
}