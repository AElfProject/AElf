namespace AElf.Crosschain.Grpc
{
    public class GrpcConfigOption
    {
        #region Local
        public bool LocalParentChainServer { get; set; }
        public string LocalParentChainNodeIp { get; set; }
        public int LocalParentChainPort { get; set; }
        public bool LocalSideChainServer { get; set; }
        public string LocalSideChainNodeIp { get; set; }
        public string LocalSideChainNodePort { get; set; }
        #endregion

        #region Remote
        public string ParentChainId { get; set; }
        public string RemoteParentChainNodeIp { get; set; }
        public int RemoteParentChainNodePort { get; set; }
        #endregion
        
        public string CertificateDir { get; set; }

    }
}