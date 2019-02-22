namespace AElf.Crosschain
{
    public interface ICrossChainCommunicationContext
    {
        string TargetIp { get; set; }
        uint TargetPort { get; set; }
        int TargetChainId { get; set; }
        ulong TargetChainHeight { get; set; }
        BlockInfoCache BlockInfoCache { get; set; }
        int ChainId { get; set; }
    }
}