using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExecutionObserverThresholdProvider
    {
        IExecutionObserverThreshold GetExecutionObserverThreshold(IBlockIndex blockIndex);
        Task SetExecutionObserverThresholdAsync(IBlockIndex blockIndex, IExecutionObserverThreshold executionObserverThreshold);
    }
    
    public class ExecutionObserverThresholdProvider : BlockExecutedDataBaseProvider<Int32Value>,
        IExecutionObserverThresholdProvider, ITransientDependency
    {
        private const string BlockExecutedDataName = "ExecutionObserverThreshold";
        private const string BranchCountThresholdKey = "BranchCountThreshold";
        private const string CallCountThresholdKey = "CallCountThreshold";

        private readonly ForkHeightOptions _forkBlockOptions;
        public ILogger<ExecutionObserverThresholdProvider> Logger { get; set; }

        public ExecutionObserverThresholdProvider(
            ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService,IOptionsSnapshot<ForkHeightOptions> forkBlockOptionsSnapshot) :
            base(cachedBlockchainExecutedDataService)
        {
            _forkBlockOptions = forkBlockOptionsSnapshot.Value;
        }

        public IExecutionObserverThreshold GetExecutionObserverThreshold(IBlockIndex blockIndex)
        {
            var defaultExecutionBranchThreshold =
                blockIndex.BlockHeight >= _forkBlockOptions.ExecutionObserverThresholdForkHeight
                    ? SmartContractConstants.ExecutionBranchThreshold
                    : SmartContractConstants.OldExecutionBranchThreshold;
            var defaultExecutionCallThreshold =
                blockIndex.BlockHeight >= _forkBlockOptions.ExecutionObserverThresholdForkHeight
                    ? SmartContractConstants.ExecutionCallThreshold
                    : SmartContractConstants.OldExecutionCallThreshold;
            
            var branchCountObserverThreshold = GetBlockExecutedData(blockIndex, BranchCountThresholdKey)?.Value ??
                                               defaultExecutionBranchThreshold;
            var callCountObserverThreshold = GetBlockExecutedData(blockIndex, CallCountThresholdKey)?.Value ??
                                             defaultExecutionCallThreshold;
            return new ExecutionObserverThreshold
            {
                ExecutionBranchThreshold = branchCountObserverThreshold,
                ExecutionCallThreshold = callCountObserverThreshold
            };
        }

        public async Task SetExecutionObserverThresholdAsync(IBlockIndex blockIndex,
            IExecutionObserverThreshold executionObserverThreshold)
        {
            if (!ValidateExecutionObserverThreshold(executionObserverThreshold))
                return;

            await AddBlockExecutedDataAsync(blockIndex, BranchCountThresholdKey,
                new Int32Value {Value = executionObserverThreshold.ExecutionBranchThreshold});
            await AddBlockExecutedDataAsync(blockIndex, CallCountThresholdKey,
                new Int32Value {Value = executionObserverThreshold.ExecutionCallThreshold});
            
            Logger.LogDebug(
                $"ExecutionObserverThreshold has been changed. Branch count threshold is {executionObserverThreshold.ExecutionBranchThreshold}. Call count threshold is {executionObserverThreshold.ExecutionCallThreshold}");
        }

        protected override string GetBlockExecutedDataName()
        {
            return BlockExecutedDataName;
        }

        private bool ValidateExecutionObserverThreshold(IExecutionObserverThreshold executionObserverThreshold)
        {
            return executionObserverThreshold.ExecutionBranchThreshold > 0 &&
                   executionObserverThreshold.ExecutionCallThreshold > 0;
        }
    }
}