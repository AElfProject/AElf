using System.Collections.Generic;
using AElf.Kernel.Types;
using CommandLine;

namespace AElf.Concurrency.Worker
{
    public class CliOptions
    {
        [Option("config.path", HelpText = "The directory the node uses to store config data.")]
        public string configPath { get; set; }
        
        #region Actor

        [Option("actor.cluster", HelpText = "Actor is cluster or not.")]
        public bool? ActorIsCluster { get; set; }

        [Option("actor.host", HelpText = "The hostname of actor.")]
        public string ActorHostName { get; set; }

        [Option("actor.port", HelpText = "The port of actor.")]
        public int? ActorPort { get; set; }

        //hide the options about concurrency cause the module haven't finished.
        [Option("actor.conlevel", Hidden = true, HelpText = "ConcurrencyLevel, used to limit the group count of the result of grouper")]
        public int? ActorConcurrencyLevel { get; set; }

        [Option("EnableParallel", Hidden = true, HelpText = "Parallel feature is disabled by default due to lack of support of calling other contracts in one contract")]
        public bool? IsParallelEnable { get; set; }

        #endregion

        #region Runner

        [Option("runner.config", HelpText = "The path to the runner config in json format.")]
        public string RunnerConfig { get; set; }

        #endregion
        
        #region Database

        // TODO: Find a way to generate help text or just use the enum DBType as its type.
        [Option('t', "db.type", HelpText = "The type of database. Must in [InMemory, Redis, SSDB].")]
        public string DBType { get; set; }

        [Option("db.host", HelpText = "The IP address of database.")]
        public string DBHost { get; set; }

        [Option("db.port", HelpText = "The port of database.")]
        public int? DBPort { get; set; }

        [Option("db.number", Default = 0, HelpText = "The number of database.")]
        public int DBNumber { get; set; }

        #endregion
    }
}