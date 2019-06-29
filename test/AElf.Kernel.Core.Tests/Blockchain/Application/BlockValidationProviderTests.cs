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
        
        public BlockValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<BlockValidationProvider>();
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
           
            block.Body.Transactions.Add(Hash.Empty);
            block.Header = new BlockHeader
            {
                Height = 10,
                PreviousBlockHash = Hash.FromString("PreviousBlockHash"),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfWorldState = Hash.FromString("MerkleTreeRootOfWorldState"),
                MerkleTreeRootOfTransactionStatus = Hash.FromString("MerkleTreeRootOfTransactionStatus"),
                MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoot(),
                BlockExtraDatas = { ByteString.CopyFromUtf8("BlockExtraData") },
                SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey")
            };
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