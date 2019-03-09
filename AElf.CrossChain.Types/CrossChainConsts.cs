namespace AElf.CrossChain
{
    public class CrossChainConsts
    {
        public const int WaitingIntervalInMillisecond = 10;
        public const int MaximalCountForIndexingParentChainBlock = 256; // Index maximal 256 blocks from parent chain.
        public const int MaximalCountForIndexingSideChainBlock = 1; // Index maximal one block from one side chain.
        public const int MinimalBlockInfoCacheThreshold = 4; // This is the biggest LIB gap actually.
        public const string RequestChainCreationMethodName = "RequestChainCreation";
        public const string CrossChainIndexingMethodName = "RecordCrossChainData";
        public const string CrossChainIndexingEventName = "CrossChainIndexingEvent";
        public const string SideChainCreationEventName = "CrossChainIndexingEvent";
        public const string GetSideChainHeightMethodName = "GetSideChainHeight";
        public const string GetParentChainHeightMethodName = "GetParentChainHeight";
        public const string GetSideChainIdAndHeightMethodName = "GetSideChainIdAndHeight";
        public const string GetAllChainsIdAndHeightMethodName = "GetAllChainsIdAndHeight";
        public const string GetParentChainIdMethodName = "GetParentChainId";
        public const string GetIndexedCrossChainBlockDataByHeight = "GetIndexedCrossChainBlockDataByHeight";
        public const string GetLockedBalanceMethodName = "LockedBalance";
        public const string GetMerklePathByHeightMethodName = "GetMerklePathByHeight";
        public const string VerifyTransactionMethodName = "VerifyTransaction";
        public const long GenesisBlockHeight = 1;
    }
}