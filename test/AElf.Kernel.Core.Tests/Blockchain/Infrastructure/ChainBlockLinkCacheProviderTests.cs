using Xunit;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class ChainBlockLinkCacheProviderTests:AElfKernelTestBase
    {
        private IChainBlockLinkCacheProvider _chainBlockLinkCacheProvider;
        public ChainBlockLinkCacheProviderTests()
        {
            _chainBlockLinkCacheProvider = GetRequiredService<IChainBlockLinkCacheProvider>();
        }

        [Fact]
        private void Get_Set_ChainBlockLink()
        {
            
        }
    }
}