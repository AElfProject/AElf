using System.Collections.Generic;

namespace AElf.Management.Models
{
    public class DeployArg
    {
        public string MainChainId { get; set; }

        public string MainChainAccount { get; set; }

        public string AccountPassword { get; set; }

        public List<string> Miners { get; set; }

        public float CpuResource { get; set; }
        
        public float MemoryResource { get; set; }

        public DeployDBArg DBArg { get; set; }

        public DeployManagerArg ManagerArg { get; set; }
        
        public DeployWorkArg WorkArg { get; set; }
        
        public DeployLauncherArg LauncherArg { get; set; }

        public DeployArg()
        {
        }
    }

    public class DeployDBArg
    {
        public string Type { get; set; }
        public int Port { get; set; }

        public DeployDBArg()
        {
            Type = "redis";
            Port = 7001;
        }
    }

    public class DeployManagerArg
    {
        public bool IsCluster { get; set; }
    }

    public class DeployWorkArg
    {
        public int WorkerCount { get; set; }

        public int ActorCount { get; set; }

        public int ConcurrencyLevel { get; set; }

        public DeployWorkArg()
        {
            WorkerCount = 1;
            ActorCount = 8;
            ConcurrencyLevel = 16;
        }
    }

    public class DeployLauncherArg
    {
        public bool IsConsensusInfoGenerator { get; set; }

        public List<string> Bootnodes { get; set; }
    }
}