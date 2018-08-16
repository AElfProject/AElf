using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AElf.Configuration
{
    [ConfigFile(FileName = "actor.json")]
    public class ActorConfig : ConfigBase<ActorConfig>
    {
        public bool IsCluster { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public int WorkerCount { get; set; }
        public bool Benchmark { get; set; }
        public List<SeedNode> Seeds { get; set; }

        public string SingleHoconFile { get; set; } = "";
        public string MasterHoconFile { get; set; } = "";
        public string WorkerHoconFile { get; set; } = "";
        public string ManagerHoconFile { get; set; } = "";

        private string _singleHoconConfig;
        private string _masterHoconConfig;
        private string _workerHoconConfig;
        private string _managerHoconConfig;

        [JsonIgnore]
        public string SingleHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_singleHoconConfig))
                    _singleHoconConfig = ReadConfFile(Instance.SingleHoconFile);
                return _singleHoconConfig;
            }
        }

        [JsonIgnore]
        public string MasterHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_masterHoconConfig))
                    _masterHoconConfig = ReadConfFile(Instance.MasterHoconFile);
                return _masterHoconConfig;
            }
        }

        [JsonIgnore]
        public string WorkerHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_workerHoconConfig))
                    _workerHoconConfig = ReadConfFile(Instance.WorkerHoconFile);
                return _workerHoconConfig;
            }
        }

        [JsonIgnore]
        public string ManagerHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_managerHoconConfig))
                    _managerHoconConfig = ReadConfFile(Instance.ManagerHoconFile);
                return _managerHoconConfig;
            }
        }

        private static string ReadConfFile(string name)
        {
            foreach (var configFilePath in ConfigManager.ConfigFilePaths)
            {
                var file = Path.Combine(configFilePath, name);
                if (!File.Exists(file)) continue;
                return File.ReadAllText(file);
            }
            throw new FileNotFoundException(name);
        }

        /// <summary>
        /// the max group count of the grouper's output
        /// see <see cref="AElf.Kernel.Concurrency.Scheduling.Grouper"/> for more details
        /// </summary>
        public int ConcurrencyLevel { get; set; }
    }

    public class SeedNode
    {
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}