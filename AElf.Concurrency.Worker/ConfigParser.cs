using AElf.Common.Application;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Runtime.CSharp;
using CommandLine;

namespace AElf.Concurrency.Worker
{
    public class ConfigParser
    {
        public bool Success { get; private set; }
        public IRunnerConfig RunnerConfig { get; private set; }

        /// <summary>
        /// chainId
        /// </summary>
        // public Hash ChainId { get; set; }
        public bool Parse(string[] args)
        {
            Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsed(opts =>
                {
                    MapOptions(opts);
                    Success = true;
                })
                .WithNotParsed((errs) => { Success = false; });

            return Success;
        }

        private void MapOptions(CliOptions opts)
        {   
            ApplicationHelpers.SetDataDir(opts.DataDir);
            // Actor
            if (opts.ActorIsCluster.HasValue)
                ActorConfig.Instance.IsCluster = opts.ActorIsCluster.Value;
            if (!string.IsNullOrWhiteSpace(opts.ActorHostName))
                ActorConfig.Instance.HostName = opts.ActorHostName;
            if (opts.ActorPort.HasValue)
                ActorConfig.Instance.Port = opts.ActorPort.Value;
            if (opts.ActorConcurrencyLevel.HasValue)
            {
                ActorConfig.Instance.ConcurrencyLevel = opts.ActorConcurrencyLevel.Value;
            }

            if (opts.IsParallelEnable.HasValue)
            {
                ParallelConfig.Instance.IsParallelEnable = opts.IsParallelEnable.Value;
            }
            
            // runner config
//            RunnerConfig = new RunnerConfig
//            {
//                SdkDir = Path.GetDirectoryName(typeof(Node.Node).Assembly.Location)
//            };
//
//            if (opts.RunnerConfig != null)
//            {
//                using (var file = File.OpenText(opts.RunnerConfig))
//                using (var reader = new JsonTextReader(file))
//                {
//                    var cfg = (JObject) JToken.ReadFrom(reader);
//                    if (cfg.TryGetValue("csharp", out var j))
//                    {
//                        RunnerConfig = Runtime.CSharp.RunnerConfig.FromJObject((JObject) j);
//                    }
//                }
//            }
        }
    }
}