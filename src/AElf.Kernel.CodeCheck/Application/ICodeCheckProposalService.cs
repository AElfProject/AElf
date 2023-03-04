using System.Collections.Generic;

namespace AElf.Kernel.CodeCheck.Application;

public interface ICodeCheckProposalService
{
    void AddToReleasedProposal(Hash proposalId, Hash proposalInputHash, long height);
    Task<List<CodeCheckProposal>> GetToReleasedProposalListAsync(Address from, Hash blockHash, long blockHeight);
    Task ClearProposalByLibAsync(Hash blockHash, long blockHeight);
}