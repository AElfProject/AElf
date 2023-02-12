namespace AElf.Contracts.Genesis;

public partial class BasicContractZero
{
    public const int ContractProposalExpirationTimePeriod = 259200; // 60 * 60 * 72
    public const int CodeCheckProposalExpirationTimePeriod = 600; // 60 * 10
    private const int MinimalApprovalThreshold = 6667;
    private const int MaximalAbstentionThreshold = 1000;
    private const int MaximalRejectionThreshold = 1000;
    private const int MinimalVoteThresholdThreshold = 8000;
}