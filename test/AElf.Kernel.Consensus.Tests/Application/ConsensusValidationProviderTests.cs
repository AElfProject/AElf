using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.Application
{
    public class ConsensusValidationProviderTests : ConsensusTestBase
    {
        private readonly IBlockValidationProvider _blockValidationProvider;
        private readonly KernelTestHelper _kernelTestHelper;

        public ConsensusValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<IBlockValidationProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task ValidateBeforeAttachAsync_Test()
        {
            var block = _kernelTestHelper.GenerateBlock(9, Hash.FromString("test"));
            block.Header = new BlockHeader
            {
                Height = 1,
                ExtraData = { }
            };
            var result = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            result.ShouldBeTrue();

            block = _kernelTestHelper.GenerateBlock(8, Hash.FromString("test"));
            result = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            result.ShouldBeFalse();

            block.Header = new BlockHeader
            {
                Height = 10,
                ExtraData = {ByteString.CopyFromUtf8("test")}
            };
            result = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBlockBeforeExecuteAsync_Test()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1,
                    ExtraData = { }
                }
            };
            var result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeTrue();

            block = _kernelTestHelper.GenerateBlock(8, Hash.FromString("test"));
            result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeFalse();

            block = _kernelTestHelper.GenerateBlock(9, Hash.FromString("test"), null, ByteString.CopyFromUtf8("extra data"));
            result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBlockAfterExecuteAsync_Test()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = 1,
                    ExtraData = { }
                }
            };
            var result = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            result.ShouldBeTrue();

            block = _kernelTestHelper.GenerateBlock(8, Hash.FromString("test"));
            result = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            result.ShouldBeFalse();

            block = _kernelTestHelper.GenerateBlock(9, Hash.FromString("test"), null, ByteString.CopyFromUtf8("extra data"));
            result = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(block);
            result.ShouldBeTrue();
        }
    }
}