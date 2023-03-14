namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckProposal
{
    public Hash ProposalId { get; set; }
    public Hash ProposedContractInputHash { get; set; }
    public long BlockHeight { get; set; }
}