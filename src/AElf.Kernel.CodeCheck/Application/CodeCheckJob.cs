namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckJob
{
    public Hash BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public byte[] ContractCode { get; set; }
    public int ContractCategory { get; set; }
    public bool IsSystemContract { get; set; }
    public bool IsUserContract { get; set; }
    public Hash CodeCheckProposalId { get; set; }
    public Hash ProposedContractInputHash { get; set; }
    public long BucketIndex { get; set; }
}