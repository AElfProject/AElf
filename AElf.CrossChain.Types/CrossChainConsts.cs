namespace AElf.CrossChain
{
    public class CrossChainConsts
    {
        public const int WaitingIntervalInMillisecond = 10;
        public const int MaximalCountForIndexingParentChainBlock = 256; // Index maximal 256 blocks from parent chain.
        public const int MaximalCountForIndexingSideChainBlock = 1; // Index maximal one block from one side chain.
        public static int MinimalBlockInfoCacheThreshold = 4; // This is the biggest LIB gap actually.
        public static string IndexingParentChainMethodName = "IndexParentChainBlockInfo";
        public static string IndexingSideChainMethodName = "IndexSideChainBlockInfo";
        public static string CrossChainIndexingMethodName = "RecordCrossChainInfo";
        public static string CrossChainIndexingEvent = "CrossChainIndexingEvent";
        public static string GetSideChainHeightMthodName = "GetSideChainHeight";
        public static string GetParentChainHeightMethodName = "GetParentChainHeight";
        public static string GetSideChainIdAndHeight = "GetSideChainIdAndHeight";
        public static string GetAllChainsIdAndHeight = "GetAllChainsIdAndHeight";
        public static string GetParentChainId = "GetParentChainId";
        public static ulong GenesisBlockHeight = 1;
    }
}