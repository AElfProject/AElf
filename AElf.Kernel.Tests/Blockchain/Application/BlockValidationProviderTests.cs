using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Common;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockValidationProviderTests : AElfKernelTestBase
    {
        private readonly BlockValidationProvider _blockValidationProvider;
        
        public BlockValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<BlockValidationProvider>();
        }

        [Fact]
        public async Task Test_Validate_Block_Before_Execute()
        {
            Block block = null;
            bool validateResult;

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block = new Block();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block.Header = new BlockHeader();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block.Body = new BlockBody();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();
            
            block.Body.Transactions.Add(Hash.Genesis);
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeFalse();

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(0, block);
            validateResult.ShouldBeTrue();        
        }

        [Fact]
        public async Task Test_Validate_Block_After_Execute()
        {
            var validateResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(0, null);
            validateResult.ShouldBeTrue();
        }
    }
}