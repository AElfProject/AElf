namespace AElf.CrossChain
{
    public class CrossChainConfigOption
    {
        public int ParentChainId { get; set; }

        public int MaximalCountForIndexingParentChainBlock { get; set; } = 32;
        public int MaximalCountForIndexingSideChainBlock { get; set; } = 32;
    }
}