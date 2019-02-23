namespace AElf.CrossChain
{
    public interface ICrossChainDataProducer
    {
//        BlockInfoCache BlockInfoCache { get; set; }
//        int ChainId { get; set; }
        bool AddNewBlockInfo(IBlockInfo blockInfo);
    }
}