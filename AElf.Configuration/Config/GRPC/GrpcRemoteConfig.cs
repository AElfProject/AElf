using System;
using System.Collections.Generic;

namespace AElf.Configuration.Config.GRPC
{
    [ConfigFile(FileName = "grpcremote.json", IsWatch = true)]
    public class GrpcRemoteConfig : ConfigBase<GrpcRemoteConfig>
    {
        public Dictionary<string, Uri> ParentChain { get; set; }
        public Dictionary<string, Uri> ChildChains { get; set; }
    }

    public class Uri
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return Address + ":" + Port;
        }
    }
}    