using System.Collections.Generic;

namespace AElf.Monitor
{
    public class ExecutionOptions
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public List<SeedNode> Seeds { get; set; }
    }
    
    public class SeedNode
    {
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}