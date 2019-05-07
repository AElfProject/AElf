namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainCommunicationDto
    {
        public string RemoteIp { get; set; }
        public int RemotePort { get; set; }
        public int RemoteChainId { get; set; }
        public int LocalChainId { get; set; }
        public bool IsClientToParentChain { get; set; }
        
        public int LocalListeningPort { get; set; }
        public int ConnectionTimeout { get; set; }

        public string ToUriStr()
        {
            return string.Join(":",RemoteIp, RemotePort);
        }
    }
}