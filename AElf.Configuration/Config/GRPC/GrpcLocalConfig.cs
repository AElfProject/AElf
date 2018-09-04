using System.Collections.Generic;
using Newtonsoft.Json;

namespace AElf.Configuration.Config.GRPC
{
    [ConfigFile(FileName = "grpclocal.json")]
    public class GrpcLocalConfig : ConfigBase<GrpcLocalConfig>
    {
        public bool IsCluster { get; set; }
        public int WaitingIntervalInMillisecond { get; set; }
        public string LocalServerIP { get; set; }
        public int LocalServerPort { get; set; }
    }
}