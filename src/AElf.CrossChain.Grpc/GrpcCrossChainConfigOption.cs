namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainConfigOption
    {
        public int ListeningPort { get; set; }
        public int ConnectionTimeout { get; set; } = 500;

        #region Parent chain
        
        public string ParentChainServerIp { get; set; }
        public int ParentChainServerPort { get; set; }
        
        #endregion
    }
}