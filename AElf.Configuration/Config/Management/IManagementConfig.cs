namespace AElf.Configuration
{
    public interface IManagementConfig
    {
        string Url { get; set; }
        string SideChainServicePath { get; set; }
        string NodeAccount { get; set; }
        string NodeAccountPassword { get; set; }
    }
}