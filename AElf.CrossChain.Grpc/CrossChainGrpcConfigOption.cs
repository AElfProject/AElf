namespace AElf.CrossChain
{
    public class CrossChainGrpcConfigOption
    {
        #region Local
        public bool LocalServer { get; set; }
        public string LocalServerIP { get; set; }
        public int LocalServerPort { get; set; }
        #endregion

        #region Remote
        public string RemoteParentChainNodeIp { get; set; }
        public int RemoteParentChainNodePort { get; set; }
        #endregion
    }
}