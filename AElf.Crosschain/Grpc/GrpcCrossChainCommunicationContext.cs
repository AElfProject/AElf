namespace AElf.Crosschain.Grpc
{
    public class GrpcCrossChainCommunicationContext : ICrossChainCommunicationContext
    {
        public string TargetIp { get; set; }
        public uint TargetPort { get; set; }
        public int TargetChainId { get; set; }
        public ulong TargetChainHeight { get; set; }
        public BlockInfoCache BlockInfoCache { get; set; }
        public int ChainId { get; set; }
        
        public string ToUriStr()
        {
            return string.Join(":",TargetIp, TargetPort);
        }
    }
}