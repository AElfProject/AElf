namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainConfigOption
    {
        #region Local
        public string LocalServerIP { get; set; }
        public int LocalServerPort { get; set; }
        #endregion

        #region Remote
        public string RemoteParentChainNodeIp { get; set; }
        public int RemoteParentChainNodePort { get; set; }
        public int ConnectingTimeout { get; set; } = 3;

        #endregion
    }
}