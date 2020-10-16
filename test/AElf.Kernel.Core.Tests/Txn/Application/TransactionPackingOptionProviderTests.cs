using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Txn.Application
{
    public class TransactionPackingOptionProviderTests : AElfKernelWithChainTestBase
    {
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public TransactionPackingOptionProviderTests()
        {
            _transactionPackingOptionProvider = GetRequiredService<ITransactionPackingOptionProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task TransactionPackingOption_Test()
        {
            await AddBlockStateSetAsync(_kernelTestHelper.BestBranchBlockList[9]);
            await AddBlockStateSetAsync(_kernelTestHelper.BestBranchBlockList[10]);
            await AddBlockStateSetAsync(_kernelTestHelper.ForkBranchBlockList[4]);
            
            var context = new ChainContext
            {
                BlockHash = _kernelTestHelper.BestBranchBlockList[9].GetHash(),
                BlockHeight = _kernelTestHelper.BestBranchBlockList[9].Height
            };
            
            var isTransactionPackable = _transactionPackingOptionProvider.IsTransactionPackable(context);
            isTransactionPackable.ShouldBeTrue();

            await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(context, false);
            
            isTransactionPackable = _transactionPackingOptionProvider.IsTransactionPackable(context);
            isTransactionPackable.ShouldBeFalse();
            
            isTransactionPackable = _transactionPackingOptionProvider.IsTransactionPackable(new ChainContext
            {
                BlockHash = _kernelTestHelper.BestBranchBlockList[10].GetHash(),
                BlockHeight = _kernelTestHelper.BestBranchBlockList[10].Height
            });
            isTransactionPackable.ShouldBeFalse();
            
            isTransactionPackable = _transactionPackingOptionProvider.IsTransactionPackable(new ChainContext
            {
                BlockHash = _kernelTestHelper.ForkBranchBlockList[4].GetHash(),
                BlockHeight = _kernelTestHelper.ForkBranchBlockList[4].Height
            });
            isTransactionPackable.ShouldBeTrue();
            
            await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(context, true);
            
            isTransactionPackable = _transactionPackingOptionProvider.IsTransactionPackable(context);
            isTransactionPackable.ShouldBeTrue();
        }

        private async Task AddBlockStateSetAsync(Block block)
        {
            var blockStateSet = new BlockStateSet
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
                PreviousHash = block.Header.PreviousBlockHash
            };
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
        }
    }
}