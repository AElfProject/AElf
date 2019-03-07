using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainValidationProviderTest : CrossChainTestBase
    {
        private readonly IBlockValidationProvider _crossChainBlockValidationProvider;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        public CrossChainValidationProviderTest()
        {
            _crossChainBlockValidationProvider = GetRequiredService<IBlockValidationProvider>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }

        [Fact]
        public void Validate_EmptyHeader()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1
                }
            };
            var res = _crossChainBlockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            
        }
        
        
    }
}