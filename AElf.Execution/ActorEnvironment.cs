using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Configuration;
using AElf.ChainController.Execution;
using AElf.Execution;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Execution
{
    public class ActorEnvironment : IActorEnvironment
    {
        private const string SystemName = "AElfSystem";
        private ActorSystem _actorSystem;
        private IActorRef _router;
        private IActorRef _requestor;
        private readonly ServicePack _servicePack;

        public bool Initialized { get; private set; }

        public Task TerminationHandle => _actorSystem.WhenTerminated;

        public IActorRef Requestor
        {
            get
            {
                if (_requestor == null)
                {
                    _requestor = _actorSystem.ActorOf(Execution.Requestor.Props(_router));    
                }

                return _requestor;
            }
        }

        public ActorEnvironment(ServicePack servicePack)
        {
            _servicePack = servicePack;
            _servicePack.WorldStateDictator.DeleteChangeBeforesImmidiately = ActorConfig.Instance.Benchmark;
            Initialized = false;
        }

        public void InitActorSystem()
        {
            if (ActorConfig.Instance.IsCluster)
            {
                var config = PrepareActorConfig(ActorConfig.Instance.MasterHoconConfig);

                _actorSystem = ActorSystem.Create(SystemName, config);
                //Todo waiting for join cluster. we should get the status here.
                Thread.Sleep(8000);
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
            }
            else
            {
                var config = PrepareActorConfig(ActorConfig.Instance.SingleHoconConfig);

                _actorSystem = ActorSystem.Create(SystemName, config);
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
                InitLocalWorkers();
            }

            Initialized = true;
        }

        public async Task StopAsync()
        {
            await CoordinatedShutdown.Get(_actorSystem).Run();
        }

        private void InitLocalWorkers()
        {
            for (var i = 0; i < ActorConfig.Instance.ActorCount; i++)
            {
                var worker = _actorSystem.ActorOf(Props.Create<Execution.Worker>(), "worker" + i);
                worker.Tell(new LocalSerivcePack(_servicePack));
            }
        }

        #region static method

        private static Config PrepareActorConfig(string content)
        {
            if (ActorConfig.Instance.Seeds == null || ActorConfig.Instance.Seeds.Count == 0)
            {
                ActorConfig.Instance.Seeds = new List<SeedNode>
                {
                    new SeedNode {HostName = ActorConfig.Instance.HostName, Port = ActorConfig.Instance.Port}
                };
            }

            var seeds = string.Join(",",
                ActorConfig.Instance.Seeds.Select(s => $@"""akka.tcp://{SystemName}@{s.HostName}:{s.Port}"""));
            var seedsString = $"akka.cluster.seed-nodes = [{seeds}]";

            var paths = string.Join(",",
                Enumerable.Range(0, ActorConfig.Instance.ActorCount).Select(i => $@"""/user/worker{i}"""));
            var pathsString = $"akka.actor.deployment./router.routees.paths = [{paths}]";

            var config = ConfigurationFactory
                .ParseString($"akka.remote.dot-netty.tcp.hostname = {ActorConfig.Instance.HostName}")
                .WithFallback(
                    ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.port = {ActorConfig.Instance.Port}"))
                .WithFallback(ConfigurationFactory.ParseString(seedsString))
                .WithFallback(ConfigurationFactory.ParseString(pathsString))
                .WithFallback(content);

            return config;
        }

        #endregion static method
    }
}