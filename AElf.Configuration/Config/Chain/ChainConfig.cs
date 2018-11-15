namespace AElf.Configuration.Config.Chain
{
    [ConfigFile(FileName = "chain.json")]
    public class ChainConfig : ConfigBase<ChainConfig>
    {
        public string ChainId { get; set; }
    }
}