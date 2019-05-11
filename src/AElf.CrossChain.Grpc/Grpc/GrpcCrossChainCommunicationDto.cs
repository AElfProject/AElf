using System;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainCommunicationDto
    {
        public string RemoteServerHost { get; set; }
        public int RemoteServerPort { get; set; }
        public int RemoteChainId { get; set; }
        public int LocalChainId { get; set; }
        public int LocalListeningPort { get; set; }
        public int ConnectionTimeout { get; set; }

        public string ToUriStr()
        {
            return $"{RemoteServerHost}:{RemoteServerPort}";
        }
    }
}