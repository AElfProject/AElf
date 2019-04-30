using System.Collections.Generic;

namespace AElf.Kernel.SmartContractExecution
{
    public class ExecutionOptions
    {
        public string ExecutorType { get; set; }
        public bool IsCluster { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public int ActorCount { get; set; }
        public List<SeedNode> Seeds { get; set; }
        /// <summary>
        /// the max group count of the grouper's output
        /// see <see cref="Grouper"/> for more details
        /// </summary>
        public int ConcurrencyLevel { get; set; }
    }
    
    public class SeedNode
    {
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}