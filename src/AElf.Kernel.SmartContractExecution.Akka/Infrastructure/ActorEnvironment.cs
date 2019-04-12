using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Execution;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.SmartContractExecution
{
    public class ActorEnvironment : IActorEnvironment
    {
        private const string SystemName = "AElfSystem";
        private ActorSystem _actorSystem;
        private IActorRef _router;
        private IActorRef _requestor;
        private readonly ServicePack _servicePack;
        private readonly ExecutionOptions _executionOptions;

        public bool Initialized { get; private set; }

        public Task TerminationHandle => _actorSystem.WhenTerminated;

        public IActorRef Requestor
        {
            get
            {
                if (_requestor == null)
                {
                    _requestor = _actorSystem.ActorOf(Akka.Infrastructure.Requestor.Props(_router));    
                }

                return _requestor;
            }
        }

        public ActorEnvironment(ServicePack servicePack, IOptionsSnapshot<ExecutionOptions> options)
        {
            _servicePack = servicePack;
            _executionOptions = options.Value;
            Initialized = false;
        }

        public void InitActorSystem()
        {
            if (_executionOptions.IsCluster)
            {
                var config = PrepareActorConfig(File.ReadAllText("akka-master.hocon"));

                _actorSystem = ActorSystem.Create(SystemName, config);
                //Todo waiting for join cluster. we should get the status here.
                Thread.Sleep(8000);
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
            }
            else
            {
                var config = PrepareActorConfig(File.ReadAllText("akka-single.hocon"));

                _actorSystem = ActorSystem.Create(SystemName, config);
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
                InitLocalWorkers();
            }

            Initialized = true;
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run(CoordinatedShutdown.ClusterLeavingReason.Instance);
        }

        private void InitLocalWorkers()
        {
            for (var i = 0; i < _executionOptions.ActorCount; i++)
            {
                var worker = _actorSystem.ActorOf(Props.Create<Worker>(), "worker" + i);
                worker.Tell(new LocalSerivcePack(_servicePack));
            }
        }

        #region static method

        private Config PrepareActorConfig(string content)
        {
            if (_executionOptions.Seeds == null || _executionOptions.Seeds.Count == 0)
            {
                _executionOptions.Seeds = new List<SeedNode>
                {
                    new SeedNode {HostName = _executionOptions.HostName, Port = _executionOptions.Port}
                };
            }

            var seeds = string.Join(",",
                _executionOptions.Seeds.Select(s => $@"""akka.tcp://{SystemName}@{s.HostName}:{s.Port}"""));
            var seedsString = $"akka.cluster.seed-nodes = [{seeds}]";

            var paths = string.Join(",",
                Enumerable.Range(0, _executionOptions.ActorCount).Select(i => $@"""/user/worker{i}"""));
            var pathsString = $"akka.actor.deployment./router.routees.paths = [{paths}]";

            var config = ConfigurationFactory
                .ParseString($"akka.remote.dot-netty.tcp.hostname = {_executionOptions.HostName}")
                .WithFallback(
                    ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.port = {_executionOptions.Port}"))
                .WithFallback(ConfigurationFactory.ParseString(seedsString))
                .WithFallback(ConfigurationFactory.ParseString(pathsString))
                .WithFallback(content);

            return config;
        }

        #endregion static method
    }
}