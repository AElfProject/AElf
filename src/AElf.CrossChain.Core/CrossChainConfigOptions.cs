namespace AElf.CrossChain
{
    public class CrossChainConfigOptions
    {
        public string ParentChainId { get; set; }

        public int MaximalCountForIndexingParentChainBlock { get; set; } =
            CrossChainConstants.DefaultBlockCacheEntityCount;

        public int MaximalCountForIndexingSideChainBlock { get; set; } =
            CrossChainConstants.DefaultBlockCacheEntityCount;

        public bool CrossChainDataValidationIgnored { get; set; } = true;
    }
}