using System.Threading.Tasks;
using AElf.Types;
using Xunit;

namespace AElf.Kernel.Blockchain.Domain
{
    public sealed class BlockManagerTests:AElfKernelTestBase
    {
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockManager _blockManager;
        private const int _chainId = 1234;

        public BlockManagerTests()
        {
            _blockManager = GetRequiredService<IBlockManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task AddBlockHeaderTest()
        {
            var header = _kernelTestHelper.GenerateBlock(0, Hash.Empty).Header;
            await _blockManager.AddBlockHeaderAsync(header);
        }

        [Fact]
        public async Task AddBlockBody_Test()
        {
            var hash = Hash.FromString("hash");
            var body = new BlockBody();
            await _blockManager.AddBlockBodyAsync(hash, body);
        }

        [Fact]
        public async Task GetBlock_Header_And_Body_Test()
        {
            var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty);
            var blockHash = block.GetHash();
            await _blockManager.AddBlockHeaderAsync(block.Header);
            var blockHeader = await _blockManager.GetBlockHeaderAsync(blockHash);
            Assert.Equal(blockHeader, block.Header);
            
            await _blockManager.AddBlockBodyAsync(blockHash, block.Body);

            var storedBlock = await _blockManager.GetBlockAsync(blockHash);
            Assert.Equal(storedBlock.Header, block.Header);
            Assert.Equal(storedBlock.Body, block.Body);
        }
    }
}