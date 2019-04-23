using System.Collections.Generic;

namespace AElf.CrossChain
{
    public class CrossChainConsts
    {
        public const int WaitingIntervalInMillisecond = 10;
        public const int MaximalCountForIndexingParentChainBlock = 256; // Index maximal 256 blocks from parent chain.
        public const int MaximalCountForIndexingSideChainBlock = 1; // Index maximal one block from one side chain.
        public const int MinimalBlockInfoCacheThreshold = 4; // This is the biggest LIB gap actually.
        public const string CrossChainIndexingMethodName = "RecordCrossChainData";
        public static readonly List<string> SymbolsOfExchangedExtraData = new List<string>{"Consensus"};
    }
}