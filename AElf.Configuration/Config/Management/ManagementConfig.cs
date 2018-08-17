namespace AElf.Configuration
{
    [ConfigFile(FileName = "management.json")]
    public class ManagementConfig : ConfigBase<ManagementConfig>
    {
        public string Url { get; set; }
        public string SideChainServicePath { get; set; }
        public string NodeAccount { get; set; }
        public string NodeAccountPassword { get; set; }
    }
}