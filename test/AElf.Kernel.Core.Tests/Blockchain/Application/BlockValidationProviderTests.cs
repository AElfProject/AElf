using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockValidationProviderTests : AElfKernelWithChainTestBase
    {
        private readonly BlockValidationProvider _blockValidationProvider;
        private readonly IBlockValidationService _blockValidationService;
        private readonly ITransactionBlockIndexService _transactionBlockIndexService;
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockValidationProviderTests()
        {
            _blockValidationService = GetRequiredService<IBlockValidationService>();
            _blockValidationProvider = GetRequiredService<BlockValidationProvider>();
            _transactionBlockIndexService = GetRequiredService<ITransactionBlockIndexService>();
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
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeFalse();

            block.Header = new BlockHeader();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeFalse();

            block.Body = new BlockBody();
            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeFalse();

            block.Body.TransactionIds.Add(Hash.Empty);
            block.Header = _kernelTestHelper.GenerateBlock(9, HashHelper.ComputeFromString("PreviousBlockHash")).Header;
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeTrue();
        }

        [Fact]
        public async Task Test_Validate_Block_After_Execute()
        {
            var validateResult = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(null);
            validateResult.ShouldBeTrue();
        }

        [Fact]
        public async Task Test_Validate_Before_Attach()
        {
            Block block = null;
            bool validateResult;

            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block = new Block();
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Header = new BlockHeader();
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Body = new BlockBody();
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Body.TransactionIds.Add(Hash.Empty);
            block.Header.ChainId = 1234;
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Header.MerkleTreeRootOfTransactions = Hash.Empty;
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Header = _kernelTestHelper.GenerateBlock(9, HashHelper.ComputeFromString("PreviousBlockHash")).Header;
            block.Header.ChainId = 0;
            block.Header.Signature =
                ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(_kernelTestHelper.KeyPair.PrivateKey,
                    block.GetHash().ToByteArray()));
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoot();
            block.Header.Time = TimestampHelper.GetUtcNow() + TimestampHelper.DurationFromMinutes(30);
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Header.Time = TimestampHelper.GetUtcNow();
            validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBlockBeforeAttachAsync_Test()
        {
            var block = _kernelTestHelper.GenerateBlock(9, HashHelper.ComputeFromString("PreviousBlockHash"));
            var validateResult = await _blockValidationService.ValidateBlockBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();

            block.Body.TransactionIds.Add(Hash.Empty);
            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoot();
            block.Header.ChainId = 0;

            block.Header.Signature =
                ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(_kernelTestHelper.KeyPair.PrivateKey,
                    block.GetHash().ToByteArray()));

            validateResult = await _blockValidationService.ValidateBlockBeforeAttachAsync(block);
            validateResult.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBeforeAttach_DuplicatesTransactions_ReturnFalse()
        {
            var transaction = _kernelTestHelper.GenerateTransaction();
            var block = _kernelTestHelper.GenerateBlock(9, HashHelper.ComputeFromString("PreviousBlockHash"),
                new List<Transaction> {transaction, transaction});

            block.Header.Signature =
                ByteString.CopyFrom(CryptoHelper.SignWithPrivateKey(_kernelTestHelper.KeyPair.PrivateKey,
                    block.GetHash().ToByteArray()));

            var validateResult = await _blockValidationProvider.ValidateBeforeAttachAsync(block);
            validateResult.ShouldBeFalse();
        }

        [Fact]
        public async Task ValidateBlockBeforeExecute_Repackaged_ReturnFalse()
        {
            var transaction = _kernelTestHelper.GenerateTransaction();
            var block = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
            {
                transaction
            });
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {transaction.GetHash()},
                new BlockIndex
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                });

            var validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeTrue();

            var repackagedBlock = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
            {
                transaction
            });

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(repackagedBlock);
            validateResult.ShouldBeFalse();
        }

        [Fact]
        public async Task ValidateBlockBeforeExecute_RepackagedInDifferentBranch_ReturnTrue()
        {
            var transaction = _kernelTestHelper.GenerateTransaction();
            var forkBranchBlock = _kernelTestHelper.ForkBranchBlockList.Last();
            var block = await _kernelTestHelper.AttachBlock(forkBranchBlock.Height, forkBranchBlock.GetHash(),
                new List<Transaction>
                {
                    transaction
                });
            await _transactionBlockIndexService.AddBlockIndexAsync(new List<Hash> {transaction.GetHash()},
                new BlockIndex
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                });

            var validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(block);
            validateResult.ShouldBeTrue();

            var repackagedBlock = await _kernelTestHelper.AttachBlockToBestChain(new List<Transaction>
            {
                transaction
            });

            validateResult = await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(repackagedBlock);
            validateResult.ShouldBeTrue();
        }
    }
}