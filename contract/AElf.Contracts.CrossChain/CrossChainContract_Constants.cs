namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private const int CrossChainIndexingProposalExpirationTimePeriod = 120;
        private const long CrossChainIndexingBannedBlockHeightInterval = 256;
        private const int SideChainCreationProposalExpirationTimePeriod = 86400; // 60 * 60 * 24
    }
}