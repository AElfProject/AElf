namespace AElf.Kernel.Node.Network
{
    public class TcpServerConfig : IAElfServerConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 6790;
    }
}