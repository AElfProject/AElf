namespace AElf.CrossChain
{
    public class CrossChainConfigOptions
    {
        public int ParentChainId { get; set; }

        public int MaximalCountForIndexingParentChainBlock { get; set; } = 32;
        public int MaximalCountForIndexingSideChainBlock { get; set; } = 32;

        public bool CrossChainDataValidationIgnored { get; set; } = true;
    }
}