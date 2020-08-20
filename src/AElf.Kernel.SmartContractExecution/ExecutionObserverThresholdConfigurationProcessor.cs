using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution
{
    public class ExecutionObserverThresholdConfigurationProcessor : IConfigurationProcessor
    {
        private readonly IExecutionObserverThresholdProvider _executionObserverThresholdProvider;

        public ExecutionObserverThresholdConfigurationProcessor(
            IExecutionObserverThresholdProvider executionObserverThresholdProvider)
        {
            _executionObserverThresholdProvider = executionObserverThresholdProvider;
        }

        public string ConfigurationName => "ExecutionObserverThreshold";
        
        public async Task ProcessConfigurationAsync(ByteString byteString, BlockIndex blockIndex)
        {
            var executionObserverBranchThreshold = new ExecutionObserverThreshold();
            executionObserverBranchThreshold.MergeFrom(byteString);
            await _executionObserverThresholdProvider.SetExecutionObserverThresholdAsync(blockIndex,
                executionObserverBranchThreshold);
        }
    }
    
    public partial class ExecutionObserverThreshold : IExecutionObserverThreshold
    {
    }
}