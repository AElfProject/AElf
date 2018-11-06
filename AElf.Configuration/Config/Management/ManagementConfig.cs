namespace AElf.Configuration
{
    [ConfigFile(FileName = "management.json")]
    public class ManagementConfig : ConfigBase<ManagementConfig>
    {
        public string Url { get; set; }
        public string SideChainServicePath { get; set; }
        
        public string DeployType { get; set; }

        public bool Authentication { get; set; }

        public int SignTimeout { get; set; }

        public int MonitoringInterval { get; set; }
    }
}