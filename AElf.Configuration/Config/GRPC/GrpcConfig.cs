using System.Collections.Generic;
using Newtonsoft.Json;

namespace AElf.Configuration.Config.GRPC
{
    [ConfigFile(FileName = "grpc.json")]
    public class GrpcConfig : ConfigBase<GrpcConfig>
    {
        public string LocalMinerServerIP { get; set; }
        public int LocalMinerServerPort { get; set; }
        public Dictionary<string, ChainIdURI> ChildChains { get; set; }
    }

    public class ChainIdURI
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }
    
}