using System;
using System.Collections.Generic;
using AElf.Configuration;

namespace AElf.Kernel.Concurrency.Execution.Config
{
    [ConfigFile(FileName = "actorconfig.json")]
    public class ActorConfig : ConfigBase<ActorConfig>
    {
        public bool IsCluster { get; set; }

        public string HostName { get; set; }

        public int Port { get; set; }

        public int WorkerCount { get; set; }

        public List<SeedNode> Seeds { get; set; }

        public ActorConfig()
        {
            IsCluster = false;
            HostName = "127.0.0.1";
            Port = 32550;
            WorkerCount = 8;
        }
    }

    public class SeedNode
    {
        public string HostName { get; set; }

        public int Port { get; set; }
    }
}