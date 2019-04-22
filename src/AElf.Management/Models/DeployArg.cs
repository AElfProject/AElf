using System.Collections.Generic;

namespace AElf.Management.Models
{
    public class DeployArg
    {
        public string MainChainId { get; set; }

        public bool IsDeployMainChain { get; set; }

        public string SideChainId { get; set; }

        public string ChainAccount { get; set; }

        public string AccountPassword { get; set; }

        public List<string> Miners { get; set; }

        public float CpuResource { get; set; }

        public float MemoryResource { get; set; }

        public string ApiKey { get; set; }

        public DeployDBArg DBArg { get; set; }

        public DeployLighthouseArg LighthouseArg { get; set; }

        public DeployWorkArg WorkArg { get; set; }

        public DeployLauncherArg LauncherArg { get; set; }

        public DeployArg()
        {
            Miners = new List<string>();
            DBArg = new DeployDBArg();
            LighthouseArg = new DeployLighthouseArg();
            WorkArg = new DeployWorkArg();
            LauncherArg = new DeployLauncherArg();
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

    public class DeployLighthouseArg
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
        public string ClusterIp { get; set; }

        public bool IsConsensusInfoGenerator { get; set; }

        public List<string> Bootnodes { get; set; }
    }
}