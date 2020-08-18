namespace AElf.CrossChain
{
    public class CrossChainConfigOptions
    {
        public string ParentChainId { get; set; }

        public bool CrossChainDataValidationIgnored { get; set; } = true;

        public int CrossChainCacheSizeLimit { get; set; } = CrossChainConstants.ChainCacheEntityCapacity;
    }
}