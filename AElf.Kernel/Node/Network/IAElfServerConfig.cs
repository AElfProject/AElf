namespace AElf.Kernel.Node.Network
{
    public interface IAElfServerConfig
    {
        string Host { get; set; }
        int Port { get; set; }
    }
}