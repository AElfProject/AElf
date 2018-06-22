namespace AElf.Kernel.Node.Config
{
    public interface INodeConfig
    {
        bool FullNode { get; set; }
        bool IsMiner { get; set; }
        Hash ChainId { get; set; }
        string DataDir { get; set; }
    }
}