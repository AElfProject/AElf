using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockValidationProviderTests : AElfKernelTestBase
    {
        private readonly BlockValidationProvider _blockValidationProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        
        public BlockValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<BlockValidationProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Test_Validate_Block_Before_Execute()
        {
            Block block = null;
            bool validateResult;

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeFalse();
            
            block = new Block();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync( block);
            validateResult.ShouldBeFalse();
            
            block.Header = new BlockHeader();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync( block);
            validateResult.ShouldBeFalse();
            
            block.Body = new BlockBody();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync( block);
            validateResult.ShouldBeFalse();
           
            block.Body.TransactionIds.Add(Hash.Empty);
            block.Header = _kernelTestHelper.GenerateBlock(9, Hash.FromString("PreviousBlockHash")).Header;
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync( block);
            validateResult.ShouldBeFalse();

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync( block);
            validateResult.ShouldBeTrue();        
        }

        [Fact]
        public async Task Test_Validate_Block_After_Execute()
        {
            var validateResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync( null);
            validateResult.ShouldBeTrue();
        }
    }
}