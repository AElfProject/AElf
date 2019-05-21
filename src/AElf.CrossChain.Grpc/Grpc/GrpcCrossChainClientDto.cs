using System;
using AElf.CrossChain.Plugin.Infrastructure;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientDto : ICrossChainClientDto
    {
        public string RemoteServerHost { private get; set; }
        public int RemoteServerPort { private get; set; }
        public int RemoteChainId { get; set; }
        public int LocalChainId { get; set; }
        public int LocalListeningPort { get; set; }
        public int ConnectionTimeout { get; set; }
        
        public bool IsClientToParentChain { get; set; }

        public string ToUriStr()
        {
            return new UriBuilder("http", RemoteServerHost, RemoteServerPort).Host;
        }
    }
}