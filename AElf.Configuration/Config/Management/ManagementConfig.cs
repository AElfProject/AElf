namespace AElf.Configuration
{
    public class ManagementConfig : IManagementConfig
    {
        public string Url { get; set; }
        public string SideChainServicePath { get; set; }
        public string NodeAccount { get; set; }
        public string NodeAccountPassword { get; set; }
    }
}