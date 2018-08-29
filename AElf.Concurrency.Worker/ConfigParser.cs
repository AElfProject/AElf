using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.ChainController;
using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Enums;
using AElf.Concurrency.Worker;
using AElf.Configuration;
using AElf.Configuration.Config.Network;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Kernel.Types;
using AElf.Runtime.CSharp;
using CommandLine;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            
            // Database
            DatabaseConfig.Instance.Type = DatabaseTypeHelper.GetType(opts.DBType);

            if (!string.IsNullOrWhiteSpace(opts.DBHost))
            {
                DatabaseConfig.Instance.Host = opts.DBHost;
            }

            if (opts.DBPort.HasValue)
            {
                DatabaseConfig.Instance.Port = opts.DBPort.Value;
            }

            DatabaseConfig.Instance.Number = opts.DBNumber;
            
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

            if (opts.Benchmark.HasValue)
            {
                ActorConfig.Instance.Benchmark = opts.Benchmark.Value;
            }

            NodeConfig.Instance.DataDir = string.IsNullOrEmpty(opts.DataDir)
                ? ApplicationHelpers.GetDefaultDataDir()
                : opts.DataDir;
            
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