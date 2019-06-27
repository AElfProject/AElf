namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainConfigOption
    {
        #region Local
        
        public string LocalServerHost { get; set; }
        public int LocalServerPort { get; set; }
        
        public string ListeningHost { get; set; } 
            
        #endregion

        #region Remote
        
        public string RemoteParentChainServerHost { get; set; }
        public int RemoteParentChainServerPort { get; set; }
        public int ConnectionTimeout { get; set; } = 500;

        #endregion
    }
}