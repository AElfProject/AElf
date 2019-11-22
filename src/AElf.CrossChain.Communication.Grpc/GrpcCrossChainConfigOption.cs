namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainConfigOption
    {
        #region Local
        
        public int ListeningPort { get; set; }
            
        #endregion

        #region Remote
        
        public string ParentChainServerIp { get; set; }
        public int ParentChainServerPort { get; set; }
        public int ConnectionTimeout { get; set; } = 500;

        #endregion
    }
}