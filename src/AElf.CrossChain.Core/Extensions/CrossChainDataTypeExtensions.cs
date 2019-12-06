using Acs7;

namespace AElf.CrossChain
{
    public static class CrossChainDataTypeExtensions
    {
        public static bool IsNullOrEmpty(this CrossChainBlockData crossChainBlockData)
        {
            return crossChainBlockData == null || crossChainBlockData.ParentChainBlockData.Count == 0 &&
                   crossChainBlockData.SideChainBlockData.Count == 0;
        }
        
        public static bool IsNullOrEmpty(this IndexedSideChainBlockData indexedSideChainBlockData)
        {
            return indexedSideChainBlockData == null || indexedSideChainBlockData.SideChainBlockData.Count == 0;
        }
    }
}