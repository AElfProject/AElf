namespace AElf.Crosschain
{
    public class CommunicationContextDto
    {
        public ICrossChainCommunicationContext CrossChainCommunicationContext { get; set; }
        public ulong TargetHeight { get; set; }
        public BlockInfoCache BlockInfoCache { get; set; }
        
        public bool IsSideChain { get; set; }
    }
}