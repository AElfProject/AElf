using System;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockValidationProviderTests : AElfKernelTestBase
    {
        private readonly BlockValidationProvider _blockValidationProvider;
        private readonly IAccountService _accountService;
        
        public BlockValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<BlockValidationProvider>();
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task Test_Validate_Block_Before_Execute()
        {
            Block block = null;
            bool validateResult;

            validateResult = await _blockValidationProvider.ValidateBlockAsync(block);
            validateResult.ShouldBeFalse();
            
            block = new Block();
            validateResult = await _blockValidationProvider.ValidateBlockAsync(block);
            validateResult.ShouldBeFalse();
            
            block.Header = new BlockHeader();
            validateResult = await _blockValidationProvider.ValidateBlockAsync(block);
            validateResult.ShouldBeFalse();
            
            block.Body = new BlockBody();
            validateResult = await _blockValidationProvider.ValidateBlockAsync(block);
            validateResult.ShouldBeFalse();
            
//            todo recover these tests
//            var signature = await _accountService.SignAsync(block.GetHash().DumpByteArray());
//            block.Header.Signature = ByteString.CopyFrom(signature);
//            
//            block.Body.Transactions.Add(Hash.Empty);
//            validateResult = await _blockValidationProvider.ValidateBlockAsync(block);
//            validateResult.ShouldBeFalse();
//
//            block.Header.Time = DateTime.UtcNow.ToTimestamp();
//            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
//            validateResult = await _blockValidationProvider.ValidateBlockAsync(block);
//            validateResult.ShouldBeTrue();        
        }

        [Fact]
        public async Task Test_Validate_Block_After_Execute()
        {
            var validateResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync( null);
            validateResult.ShouldBeTrue();
        }
    }
}