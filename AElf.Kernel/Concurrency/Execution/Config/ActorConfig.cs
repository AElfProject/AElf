using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Execution.Config
{
    [ConfigFile(FileName = "actorconfig.json")]
    public class ActorConfig : ConfigBase<ActorConfig>
    {
        public bool IsCluster { get; set; }

        public string HostName { get; set; }

        public int Port { get; set; }

        public string SeedHostName { get; set; }

        public int SeedPort { get; set; }

        public int WorkerCount { get; set; }

        public ActorConfig()
        {
            IsCluster = false;
            HostName = "127.0.0.1";
            Port = 0;
            SeedHostName ="127.0.0.1";
            SeedPort = 32551;
            WorkerCount = 8;
        }
    }
}