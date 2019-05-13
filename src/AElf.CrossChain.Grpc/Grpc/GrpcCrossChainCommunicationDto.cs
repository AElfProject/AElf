using System;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainCommunicationDto
    {
        public string RemoteServerHost { private get; set; }
        public int RemoteServerPort { private get; set; }
        public int RemoteChainId { get; set; }
        public int LocalChainId { get; set; }
        public int LocalListeningPort { get; set; }
        public int ConnectionTimeout { get; set; }

        public string ToUriStr()
        {
            return new UriBuilder("http", RemoteServerHost, RemoteServerPort).Uri.Authority;
        }
    }
}