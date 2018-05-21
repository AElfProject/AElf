namespace AElf.Kernel.Node.Network.Config
{
    public class TcpServerConfig : IAElfServerConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 6790;
    }
}