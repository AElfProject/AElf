using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner.Application
{
    public class BlockValidationProviderTests : KernelWithChainTestBase
    {
        private readonly BlockValidationProvider _blockValidationProvider;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public BlockValidationProviderTests()
        {
            _blockValidationProvider = GetRequiredService<BlockValidationProvider>();
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task ValidateBlockBeforeExecute_Test()
        {
            var previousBlockHash = _kernelTestHelper.BestBranchBlockList.Last().GetHash();
            var previousBlockHeight = _kernelTestHelper.BestBranchBlockList.Last().Height;
            var blockWithTransaction2 = GenerateBlock(previousBlockHeight, previousBlockHash, 2);
            var blockWithTransaction3 = GenerateBlock(previousBlockHeight, previousBlockHash, 3);
            var blockWithTransaction4 = GenerateBlock(previousBlockHeight, previousBlockHash, 4);
            await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
            {
                BlockHash = previousBlockHash,
                BlockHeight = previousBlockHeight
            });

            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction2)).ShouldBeTrue();
            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction3)).ShouldBeTrue();
            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction4)).ShouldBeTrue();

            await _blockTransactionLimitProvider.SetLimitAsync(new BlockIndex
            {
                BlockHeight = previousBlockHeight,
                BlockHash = previousBlockHash
            }, 3);

            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction2)).ShouldBeTrue();
            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction3)).ShouldBeTrue();
            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction4)).ShouldBeFalse();

            await _blockTransactionLimitProvider.SetLimitAsync(new BlockIndex
            {
                BlockHeight = previousBlockHeight,
                BlockHash = previousBlockHash
            }, 4);

            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction2)).ShouldBeTrue();
            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction3)).ShouldBeTrue();
            (await _blockValidationProvider.ValidateBlockBeforeExecuteAsync(blockWithTransaction4)).ShouldBeTrue();
        }

        private Block GenerateBlock(long previousBlockHeight, Hash previousBlockHash, int transactionCount)
        {
            var transactions = _kernelTestHelper.GenerateTransactions(transactionCount);
            return _kernelTestHelper.GenerateBlock(previousBlockHeight, previousBlockHash, transactions);
        }

        [Fact]
        public async Task ValidateBeforeAttach_Test()
        {
            var result = await _blockValidationProvider.ValidateBeforeAttachAsync(null);
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateBlockAfterExecute_Test()
        {
            var result = await _blockValidationProvider.ValidateBlockAfterExecuteAsync(null);
            result.ShouldBeTrue();
        }
    }
}