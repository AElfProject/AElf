namespace AElf.CrossChain
{
    public class CrossChainDataProducer : ICrossChainDataProducer
    {
//        public string TargetIp { get; set; }
//        public uint TargetPort { get; set; }
//        public int SideChainId { get; set; }
        public ulong TargetChainHeight { get; set; }
//        public bool TargetIsSideChain { get; set; }
        public BlockInfoCache BlockInfoCache { get; set; }
        public int ChainId { get; set; }
       
        private readonly IMultiChainBlockInfoCache _multiChainBlockInfoCache;

        public CrossChainDataProducer(IMultiChainBlockInfoCache multiChainBlockInfoCache)
        {
            _multiChainBlockInfoCache = multiChainBlockInfoCache;
        }

        public bool AddNewBlockInfo(IBlockInfo blockInfo)
        {
            var blockInfoCache = _multiChainBlockInfoCache.GetBlockInfoCache(blockInfo.ChainId);

            if (blockInfo.Height != blockInfoCache.TargetChainHeight)
                return false;
            var res = blockInfoCache.TryAdd(blockInfo);
            return res;
        }
    }
}