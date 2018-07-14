using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Config;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;

namespace AElf.Kernel.Concurrency
{
    public class ConcurrencyExecutingService : IConcurrencyExecutingService
    {
        private ActorSystem _actorSystem;
        private IActorRef _requestor;
        private IActorRef _router;
        private bool _isInit;
        private const string SystemName = "AElfSystem";
        private readonly ServicePack _servicePack;

        public ConcurrencyExecutingService(IChainContextService chainContextService, ISmartContractService smartContractService, IFunctionMetadataService functionMetadataService, IWorldStateDictator worldStateDictator, IAccountContextService accountContextService)
        {
            _servicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = smartContractService,
                ResourceDetectionService = new ResourceUsageDetectionService(functionMetadataService),
                WorldStateDictator = worldStateDictator,
                AccountContextService = accountContextService,
            };
            _servicePack.WorldStateDictator.DeleteChangeBeforesImmidiately = ActorConfig.Instance.Benchmark;
            _isInit = false;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId, IGrouper grouper)
        {
            if (!_isInit)
            {
                InitActorSystem();
            }

            _requestor = _actorSystem.ActorOf(Requestor.Props(_router));
            var executeService = new ParallelTransactionExecutingService(_requestor, grouper);
            return await executeService.ExecuteAsync(transactions, chainId);
        }

        public void InitWorkActorSystem()
        {
            var config = InitActorConfig(ActorHocon.ActorWorkerHocon);

            _actorSystem = ActorSystem.Create(SystemName, config);
            for (var i = 0; i < ActorConfig.Instance.WorkerCount; i++)
            {
                var worker = _actorSystem.ActorOf(Props.Create<Worker>(), "worker" + i);
                worker.Tell(new LocalSerivcePack(_servicePack));
            }
        }

        public void InitActorSystem()
        {
            if (ActorConfig.Instance.IsCluster)
            {
                var config = InitActorConfig(ActorHocon.ActorClusterHocon);
                
                var workerConfigs = new StringBuilder();
                workerConfigs.Append("akka.actor.deployment./router.routees.paths = [");
                for (var i = 0; i < ActorConfig.Instance.WorkerCount; i++)
                {
                    workerConfigs.Append("\"/user/worker" + i).Append("\"").Append(",");
                }
                workerConfigs.Remove(workerConfigs.Length - 1, 1);
                workerConfigs.Append("]");

                config = ConfigurationFactory.ParseString(workerConfigs.ToString()).WithFallback(config);
                
                _actorSystem = ActorSystem.Create(SystemName, config);
                
                //Todo waiting for join cluster. we should get the status here.
                Thread.Sleep(8000);
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
            }
            else
            {
                _actorSystem = ActorSystem.Create(SystemName);
                var workers = new List<string>();
                for (var i = 0; i < ActorConfig.Instance.WorkerCount; i++)
                {
                    workers.Add("/user/worker" + i);
                }

                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
                for (var i = 0; i < ActorConfig.Instance.WorkerCount; i++)
                {
                    var worker = _actorSystem.ActorOf(Props.Create<Worker>(), "worker" + i);
                    worker.Tell(new LocalSerivcePack(_servicePack));
                }
            }

            _isInit = true;
        }

        private Akka.Configuration.Config InitActorConfig(string content)
        {
            if (ActorConfig.Instance.Seeds == null || ActorConfig.Instance.Seeds.Count == 0)
            {
                ActorConfig.Instance.Seeds = new List<SeedNode> {new SeedNode {HostName = ActorConfig.Instance.HostName, Port = ActorConfig.Instance.Port}};
            }

            var seedNodes = new StringBuilder();
            seedNodes.Append("akka.cluster.seed-nodes = [");
            foreach (var seed in ActorConfig.Instance.Seeds)
            {
                seedNodes.Append("\"akka.tcp://").Append(SystemName).Append("@").Append(seed.HostName).Append(":").Append(seed.Port).Append("\",");
            }
            seedNodes.Remove(seedNodes.Length - 1, 1);
            seedNodes.Append("]");
            
            
            var config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ActorConfig.Instance.HostName)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + ActorConfig.Instance.Port))
                .WithFallback(ConfigurationFactory.ParseString(seedNodes.ToString()))
                .WithFallback(content);

            return config;
        }
    }
}