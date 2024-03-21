using System.Collections.Generic;
using AElf.Kernel.CodeCheck.Application;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public interface ICodeCheckProposalProvider
{
    void AddProposal(Hash proposalId, Hash proposalInputHash, long height);
    IEnumerable<CodeCheckProposal> GetAllProposals();
    bool TryGetProposalCreatedHeight(Hash proposalId, out long height);
    void RemoveProposalById(Hash proposalId);
}