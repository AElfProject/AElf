using System.Threading.Tasks;
using AElf.Kernel.Services;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class BlockchainServiceTests : AElfKernelTestBase
    {
        private IBlockchainService _blockchainService;

        public BlockchainServiceTests()
        {
            _blockchainService = GetRequiredService<IFullBlockchainService>();
        }

        [Fact]
        public async Task TestAddBlock()
        {
            
        }
    }
}