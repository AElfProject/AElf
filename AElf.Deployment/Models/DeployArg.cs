namespace AElf.Deployment.Models
{
    public class DeployArg
    {
        public string MainChainAccount { get; set; }

        public string AccountPassword { get; set; }

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
        
    }

    public class DeployWorkArg
    {
        public int ActorCount { get; set; }

        public int ConcurrencyLevel { get; set; }

        public DeployWorkArg()
        {
            ActorCount = 8;
            ConcurrencyLevel = 16;
        }
    }

    public class DeployLauncherArg
    {
        
    }
}