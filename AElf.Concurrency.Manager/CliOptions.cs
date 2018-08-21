using System.Collections.Generic;
using CommandLine;

namespace AElf.Concurrency.Manager
{
    public class CliOptions
    {
        [Option("node.datadir", HelpText = "The directory the node uses to store data.")]
        public string DataDir { get; set; }
        
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

        [Option("actor.benchmark", Hidden = true, HelpText = "")]
        public bool? Benchmark { get; set; }

        #endregion

    }
}