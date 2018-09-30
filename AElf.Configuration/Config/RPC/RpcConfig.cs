namespace AElf.Configuration.Config.RPC
{
    [ConfigFile(FileName = "rpc.json")]
    public class RpcConfig:ConfigBase<RpcConfig>
    {
        public bool UseRpc { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }
    }
}