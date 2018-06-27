using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Config;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using NLog;

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
            _isInit = false;
        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId, IGrouper grouper)
        {
            if (!_isInit)
            {
                InitActorSystem();
                _isInit = true;
            }

            _requestor = _actorSystem.ActorOf(Requestor.Props(_router));
            var executeService = new ParallelTransactionExecutingService(_requestor, grouper);
            return await executeService.ExecuteAsync(transactions, chainId);
        }

        public void InitWorkActorSystem()
        {
            var config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ActorWorkerConfig.Instance.HostName)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + ActorWorkerConfig.Instance.Port))
                .WithFallback(ActorWorkerConfig.Instance.HoconContent);
            if (ActorWorkerConfig.Instance.IsSeedNode)
            {
                config = ConfigurationFactory.ParseString("akka.cluster.seed-nodes = [\"akka.tcp://" + SystemName + "@" + ActorWorkerConfig.Instance.HostName + ":" + ActorWorkerConfig.Instance.Port + "\"]")
                    .WithFallback(config);
            }

            _actorSystem = ActorSystem.Create(SystemName, config);
            var worker = _actorSystem.ActorOf(Props.Create<Worker>(), "worker");
            worker.Tell(new LocalSerivcePack(_servicePack));
        }

        private void InitActorSystem()
        {
            var config = ConfigurationFactory.ParseString(ActorConfig.Instance.HoconContent);
            if (ActorConfig.Instance.IsCluster)
            {
                config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ActorConfig.Instance.HostName)
                    .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + ActorConfig.Instance.Port))
                    .WithFallback(config);
            }
            _actorSystem = ActorSystem.Create(SystemName, config);
            if (ActorConfig.Instance.IsCluster)
            {
                //Todo waiting for join cluster. we should get the status here.
                Thread.Sleep(2000);
            }
            else
            {
                foreach (var name in config.GetStringList("akka.actor.deployment./router.routees.paths"))
                {
                    var worker = _actorSystem.ActorOf(Props.Create<Worker>(), name.Split('/').Last());
                    worker.Tell(new LocalSerivcePack(_servicePack));
                }
            }
            _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
        }
    }
}