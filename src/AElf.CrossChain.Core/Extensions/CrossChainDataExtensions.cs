using Acs7;

namespace AElf.CrossChain
{
    public static class CrossChainDataExtensions
    {
        public static bool IsNullOrEmpty(this CrossChainBlockData crossChainBlockData)
        {
            return crossChainBlockData == null || crossChainBlockData.ParentChainBlockData.Count == 0 &&
                   crossChainBlockData.SideChainBlockData.Count == 0;
        }
    }
}