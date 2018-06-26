using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Config;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;

namespace AElf.Kernel.Concurrency
{
    public class ConcurrencyExecutingService : IConcurrencyExecutingService
    {
        private ActorSystem _actorSystem;
        private readonly IGrouper _grouper;
        private IActorRef _requestor;
        private IActorRef _router;
        private bool _isInit;
        private const string SystemName = "AElfSystem";
        
        public ConcurrencyExecutingService()
        {
            _isInit = false;
        }

//        public ConcurrencyExecutingService(IGrouper grouper)
//        {
              //_isInit = false;
//            _grouper = grouper;
//            InitActorSystem();
//        }

        public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId)
        {
            if (!_isInit)
            {
                InitActorSystem();
                _isInit = true;
            }

            _requestor = _actorSystem.ActorOf(Requestor.Props(_router));
            var executeService = new ParallelTransactionExecutingService(_requestor, _grouper);
            return await executeService.ExecuteAsync(transactions, chainId);
        }

        public void InitWorkActorSystem(string ip,int port)
        {
            var config = ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ip)
                    .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + port))
                    .WithFallback(ActorWorkerConfig.Instance.HoconContent);
            if (ActorWorkerConfig.Instance.IsSeedNode)
            {
                config = ConfigurationFactory.ParseString("akka.cluster.seed-nodes = [\"akka.tcp://" + SystemName + "@" + ip + ":" + port + "\"]")
                    .WithFallback(config);
            }

            _actorSystem = ActorSystem.Create(SystemName,config);
            _actorSystem.ActorOf(Props.Create<Worker>(), "worker");
        }
        
        private void InitActorSystem()
        {
            if (ActorConfig.Instance.IsCluster)
            {
                var config = ConfigurationFactory.ParseString(ActorConfig.Instance.HoconContent);
                _actorSystem = ActorSystem.Create(SystemName,config);
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "router");
            }
            else
            {
                _actorSystem = ActorSystem.Create(SystemName);
                var workers = new List<string>(ActorConfig.Instance.WorkerNames.Count);
                foreach (var name in ActorConfig.Instance.WorkerNames)
                {
                    _actorSystem.ActorOf(Props.Create<Worker>(), name);
                    workers.Add("/user/" + name);
                }           
                _router = _actorSystem.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers.ToArray())));
            }
        }
    }
}