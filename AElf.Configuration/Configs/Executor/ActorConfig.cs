using System;
using System.Collections.Generic;
using System.IO;

namespace AElf.Configuration
{
    [ConfigFile(FileName = "actor.json")]
    public class ActorConfig : ConfigBase<ActorConfig>
    {
        public bool IsCluster { get; set; }
        public string HostName { get; set; }
        public int Port { get; set; }
        public int WorkerCount { get; set; }
        public List<SeedNode> Seeds { get; set; }

        public string SingleHoconFile { get; set; }
        public string MasterHoconFile { get; set; }
        public string WorkerHoconFile { get; set; }

        private string _singleHoconConfig;
        private string _masterHoconConfig;
        private string _workerHoconConfig;

        public string SingleHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_singleHoconConfig))
                    _singleHoconConfig = ReadConfFile(Instance.SingleHoconFile);
                return _singleHoconConfig;
            }
        }

        public string MasterHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_masterHoconConfig))
                    _masterHoconConfig = ReadConfFile(Instance.MasterHoconFile);
                Console.WriteLine(_masterHoconConfig);
                return _masterHoconConfig;
            }
        }

        public string WorkerHoconConfig
        {
            get
            {
                if (string.IsNullOrEmpty(_workerHoconConfig))
                    _workerHoconConfig = ReadConfFile(Instance.WorkerHoconFile);
                return _workerHoconConfig;
            }
        }

        private string ReadConfFile(string name)
        {
            var file = Path.Combine(ConfigManager.ConfigFilePath, name);
            if (File.Exists(file)) return File.ReadAllText(file);
            _logger.Error("Config file not exist: {0}", file);
            return "";
        }

        /// <summary>
        /// the max group count of the grouper's output
        /// see <see cref="AElf.Kernel.Concurrency.Scheduling.Grouper"/> for more details
        /// </summary>
        public int ConcurrencyLevel { get; set; }

        public ActorConfig()
        {
            IsCluster = false;
            HostName = "127.0.0.1";
            Port = 32550;
            WorkerCount = 8;
            ConcurrencyLevel = 8;
        }
    }

    public class SeedNode
    {
        public string HostName { get; set; }
        public int Port { get; set; }
    }
}