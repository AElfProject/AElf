namespace AElf.Contracts.ParliamentAuth
{
    public partial class ParliamentAuthContract
    {
        private const int AbstractDefaultOrganizationMinimalApprovalThreshold = 6666;
        private const int AbstractDefaultOrganizationMaximalAbstentionThreshold = 2000;
        private const int AbstractDefaultOrganizationMaximalRejectionThreshold = 3333;
        private const int AbstractDefaultOrganizationMinimalVoteThresholdThreshold = 8000;
        private const int AbstractVoteTotal = 10000;
    }
}