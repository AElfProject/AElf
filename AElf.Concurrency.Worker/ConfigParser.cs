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
            ApplicationHelpers.SetConfigPath(opts.configPath);
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
        }
    }
}