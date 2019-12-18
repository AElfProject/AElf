namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private const int CrossChainIndexingProposalExpirationTimeLimit = 120;
        private const long CrossChainIndexingBannedBlockHeightInterval = 256;
        private const int SideChainCreationProposalExpirationTimeLimit = 86400;
    }
}