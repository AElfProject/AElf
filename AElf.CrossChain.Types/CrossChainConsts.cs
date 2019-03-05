namespace AElf.CrossChain
{
    public class CrossChainConsts
    {
        public const int WaitingIntervalInMillisecond = 10;
        public const int MaximalCountForIndexingParentChainBlock = 256; // Index maximal 256 blocks from parent chain.
        public const int MaximalCountForIndexingSideChainBlock = 1; // Index maximal one block from one side chain.
        public static int MinimalBlockInfoCacheThreshold = 4; // This is the biggest LIB gap actually.
        public const string IndexingParentChainMethodName = "IndexParentChainBlockInfo";
        public const string IndexingSideChainMethodName = "IndexSideChainBlockInfo";
        public const string CrossChainIndexingMethodName = "RecordCrossChainData";
        public const string CrossChainIndexingEventName = "CrossChainIndexingEvent";
        public const string SideChainCreationEventName = "CrossChainIndexingEvent";
        public const string GetSideChainHeightMthodName = "GetSideChainHeight";
        public const string GetParentChainHeightMethodName = "GetParentChainHeight";
        public const string GetSideChainIdAndHeight = "GetSideChainIdAndHeight";
        public const string GetAllChainsIdAndHeight = "GetAllChainsIdAndHeight";
        public const string GetParentChainId = "GetParentChainId";
        public const string GetIndexedCrossChainBlockDataByHeight = "GetIndexedCrossChainBlockDataByHeight";
        public const ulong GenesisBlockHeight = 1;
    }
}