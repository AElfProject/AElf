using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class ExecutionObserverThresholdProviderTests : AElfIntegratedTest<SmartContractTestAElfModule>
    {
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IExecutionObserverThresholdProvider _executionObserverThresholdProvider;

        public ExecutionObserverThresholdProviderTests()
        {
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _executionObserverThresholdProvider = GetRequiredService<IExecutionObserverThresholdProvider>();
        }

        [Fact]
        public async Task ExecutionObserverThresholdProvider_GetAndSet_Test()
        {
            var blockIndex = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 1
            };

            await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            });

            {
                var executionObserverThreshold =
                    _executionObserverThresholdProvider.GetExecutionObserverThreshold(blockIndex);
                executionObserverThreshold.ExecutionBranchThreshold.ShouldBe(SmartContractConstants
                    .ExecutionBranchThreshold);
                executionObserverThreshold.ExecutionCallThreshold.ShouldBe(
                    SmartContractConstants.ExecutionCallThreshold);
            }
            
            var newExecutionObserverThreshold = new ExecutionObserverThreshold
            {
                ExecutionBranchThreshold = 1,
                ExecutionCallThreshold = 1
            };
            
            {
                await _executionObserverThresholdProvider.SetExecutionObserverThresholdAsync(blockIndex,
                    newExecutionObserverThreshold);
                var executionObserverThreshold =
                    _executionObserverThresholdProvider.GetExecutionObserverThreshold(blockIndex);
                executionObserverThreshold.ShouldBe(newExecutionObserverThreshold);
            }

            var blockIndex2 = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash1"),
                BlockHeight = blockIndex.BlockHeight + 1
            };
            
            var blockStateSet2 = new BlockStateSet
            {
                PreviousHash = blockIndex.BlockHash,
                BlockHash = blockIndex2.BlockHash,
                BlockHeight = blockIndex2.BlockHeight + 1
            };
            
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet2);
            
            {
                var executionObserverThreshold =
                    _executionObserverThresholdProvider.GetExecutionObserverThreshold(blockIndex2);
                executionObserverThreshold.ShouldBe(newExecutionObserverThreshold);
            }
            
            {
                var invalidThreshold = new ExecutionObserverThreshold
                {
                    ExecutionBranchThreshold = 1
                };
                await _executionObserverThresholdProvider.SetExecutionObserverThresholdAsync(blockIndex2,
                    invalidThreshold);
                var executionObserverThreshold =
                    _executionObserverThresholdProvider.GetExecutionObserverThreshold(blockIndex2);
                executionObserverThreshold.ShouldBe(newExecutionObserverThreshold);
            }
            
            {
                var invalidThreshold = new ExecutionObserverThreshold
                {
                    ExecutionCallThreshold = 1
                };
                await _executionObserverThresholdProvider.SetExecutionObserverThresholdAsync(blockIndex2,
                    invalidThreshold);
                var executionObserverThreshold =
                    _executionObserverThresholdProvider.GetExecutionObserverThreshold(blockIndex2);
                executionObserverThreshold.ShouldBe(newExecutionObserverThreshold);
            }
            
            {
                var validThreshold = new ExecutionObserverThreshold
                {
                    ExecutionCallThreshold = 2,
                    ExecutionBranchThreshold = 2
                };
                await _executionObserverThresholdProvider.SetExecutionObserverThresholdAsync(blockIndex2,
                    validThreshold);
                var executionObserverThreshold =
                    _executionObserverThresholdProvider.GetExecutionObserverThreshold(blockIndex2);
                executionObserverThreshold.ShouldBe(validThreshold);
            }
        }
    }
}