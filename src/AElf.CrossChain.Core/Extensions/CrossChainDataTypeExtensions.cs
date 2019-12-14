using Acs7;

namespace AElf.CrossChain
{
    public static class CrossChainDataTypeExtensions
    {
        public static bool IsNullOrEmpty(this CrossChainBlockData crossChainBlockData)
        {
            return crossChainBlockData == null || crossChainBlockData.ParentChainBlockDataList.Count == 0 &&
                   crossChainBlockData.SideChainBlockDataList.Count == 0;
        }
        
        public static bool IsNullOrEmpty(this IndexedSideChainBlockData indexedSideChainBlockData)
        {
            return indexedSideChainBlockData == null || indexedSideChainBlockData.SideChainBlockDataList.Count == 0;
        }
    }
}