namespace AElf.Contracts.Parliament
{
    public partial class ParliamentContract
    {
        private const int DefaultOrganizationMinimalApprovalThreshold = 6667;
        private const int DefaultOrganizationMaximalAbstentionThreshold = 2000;
        private const int DefaultOrganizationMaximalRejectionThreshold = 2000;
        private const int DefaultOrganizationMinimalVoteThresholdThreshold = 7500;
        private const int AbstractVoteTotal = 10000;
    }
}