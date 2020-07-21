using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class ExecutionObserverThresholdProvider : BlockExecutedDataBaseProvider<Int32Value>,
        IExecutionObserverThresholdProvider, ITransientDependency
    {
        private const string BlockExecutedDataName = "ExecutionObserverThreshold";

        public ILogger<ExecutionObserverThresholdProvider> Logger { get; set; }

        public ExecutionObserverThresholdProvider(
            ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService) :
            base(cachedBlockchainExecutedDataService)
        {
        }

        public IExecutionObserverThreshold GetExecutionObserverThreshold(IChainContext chainContext)
        {
            var branchCountObserverThreshold = GetBlockExecutedData(chainContext, "BranchCount")?.Value ??
                                               SmartContractConstants.ExecutionBranchThreshold;
            var callCountObserverThreshold = GetBlockExecutedData(chainContext, "Call")?.Value ??
                                             SmartContractConstants.ExecutionBranchThreshold;
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

            await AddBlockExecutedDataAsync(blockIndex, "BranchCount",
                new Int32Value {Value = executionObserverThreshold.ExecutionBranchThreshold});
            await AddBlockExecutedDataAsync(blockIndex, "CallCount",
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