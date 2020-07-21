// using System.Threading.Tasks;
// using AElf.Kernel.SmartContract;
// using AElf.Kernel.SmartContract.Application;
// using AElf.Kernel.SmartContract.Infrastructure;
// using Google.Protobuf.WellKnownTypes;
// using Microsoft.Extensions.Logging;
// using Volo.Abp.DependencyInjection;
//
// namespace AElf.Kernel.SmartContractExecution.Infrastructure
// {
//     public class ExecutionObserverThresholdProvider : BlockExecutedDataBaseProvider<Int32Value>,
//         IExecutionObserverThresholdProvider, ITransientDependency
//     {
//         private const string BlockExecutedDataName = "ExecutionObserverThreshold";
//
//         public ILogger<ExecutionObserverThresholdProvider> Logger { get; set; }
//
//         public ExecutionObserverThresholdProvider(
//             ICachedBlockchainExecutedDataService<ExecutionObserverThreshold> cachedBlockchainExecutedDataService) :
//             base(cachedBlockchainExecutedDataService)
//         {
//         }
//
//         public IExecutionObserverThreshold GetExecutionObserverThreshold(IChainContext chainContext)
//         {
//             var executionObserverThreshold = GetBlockExecutedData(chainContext, "BranchCount") ?? new ExecutionObserverThreshold
//             {
//                 ExecutionBranchThreshold =
//                     SmartContractConstants.ExecutionBranchThreshold,
//                 ExecutionCallThreshold = SmartContractConstants.ExecutionCallThreshold
//             };
//
//             return executionObserverThreshold;
//         }
//
//         public async Task SetExecutionObserverThresholdAsync(IBlockIndex blockIndex,
//             IExecutionObserverThreshold executionObserverThreshold)
//         {
//             if (!ValidateExecutionObserverThreshold(executionObserverThreshold))
//                 return;
//             await AddBlockExecutedDataAsync(blockIndex, (ExecutionObserverThreshold) executionObserverThreshold);
//             Logger.LogDebug(
//                 $"ExecutionObserverThreshold has been changed. Branch count threshold is {executionObserverThreshold.ExecutionBranchThreshold}. Call count threshold is {executionObserverThreshold.ExecutionCallThreshold}");
//         }
//
//         protected override string GetBlockExecutedDataName()
//         {
//             return BlockExecutedDataName;
//         }
//
//         private bool ValidateExecutionObserverThreshold(IExecutionObserverThreshold executionObserverThreshold)
//         {
//             return executionObserverThreshold.ExecutionBranchThreshold > 0 &&
//                    executionObserverThreshold.ExecutionCallThreshold > 0;
//         }
//     }
// }