using System;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class NewChainConnectionEvent
    {
        public string RemoteServerHost { get; set; }
        public int RemoteServerPort { get; set; }
        public int RemoteChainId { get; set; }
        
    }
}