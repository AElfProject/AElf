namespace AElf.Crosschain.Grpc
{
    public class GrpcConfigOption
    {
        public string ParentChainNodeIp { get; set; }
        public string ParentChainPort { get; set; }
        public string ParentChainId { get; set; }

        public string CertificateDir { get; set; }
    }
}