namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private const int CrossChainIndexingProposalExpirationTimePeriod = 120;
        private const long CrossChainIndexingBannedBlockHeightInterval = 256;
        private const int SideChainCreationProposalExpirationTimePeriod = 86400; // 60 * 60 * 24
        private const int DefaultMinimalApprovalThreshold = 6667;
        private const int DefaultMaximalAbstentionThreshold = 1000;
        private const int DefaultMaximalRejectionThreshold = 1000;
        private const int DefaultMinimalVoteThresholdThreshold = 6667;
    }
}