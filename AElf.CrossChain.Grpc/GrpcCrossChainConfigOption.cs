namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainConfigOption
    {
        #region Local
        public bool LocalServer { get; set; }
        
        public bool LocalClient { get; set; }
        public string LocalServerIP { get; set; }
        public int LocalServerPort { get; set; }

        public string LocalCertificateFileName { get; set; }
        #endregion

        #region Remote
        public string RemoteParentChainNodeIp { get; set; }
        public int RemoteParentChainNodePort { get; set; }
        public string RemoteParentCertificateFileName { get; set; }
        #endregion
    }
}