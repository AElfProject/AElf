using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests;
using AElf.Runtime.CSharp;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;
using Autofac;
using Google.Protobuf;
using NLog;
using ServiceStack;

namespace AElf.Benchmark
{
    public class Benchmarks
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly IBlockManager _blockManager;
        private readonly ISmartContractService _smartContractService;
        //private readonly IParallelTransactionExecutingService _parallelTransactionExecutingService;
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly IConcurrencyExecutingService _concurrencyExecutingService;
        
//        public ActorSystem Sys { get; } = ActorSystem.Create("benchmark");
//        public IActorRef Router { get;  }
//        public IActorRef[] Workers { get; }
//        public IActorRef Requestor { get; }

        private readonly ServicePack _servicePack;

        private readonly TransactionDataGenerator _dataGenerater;
        private readonly Hash _contractHash;

        public Benchmarks(IChainCreationService chainCreationService, IBlockManager blockManager, IChainContextService chainContextService, int maxTxNum, ISmartContractService smartContractService, ILogger logger, IFunctionMetadataService functionMetadataService
            , IAccountContextService accountContextService, ILogger logger1, IWorldStateDictator worldStateDictator,IConcurrencyExecutingService concurrencyExecutingService)
        {
            ChainId = Hash.Generate();
            
            _worldStateDictator = worldStateDictator;
            _worldStateDictator.SetChainId(ChainId).DeleteChangeBeforesImmidiately = true;
            
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractService = smartContractService;
            _accountContextService = accountContextService;
            _logger = logger1;
            _concurrencyExecutingService = concurrencyExecutingService;


            _servicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = _smartContractService,
                ResourceDetectionService = new ResourceUsageDetectionService(functionMetadataService),
                WorldStateDictator = _worldStateDictator,
                AccountContextService = _accountContextService,
            };

//            var workers = new[]
//            {
//                  "/user/worker1", "/user/worker2", 
//                  "/user/worker3", "/user/worker4",
//                  "/user/worker5", "/user/worker6",
//                  "/user/worker7", "/user/worker8",
//                  "/user/worker9", "/user/worker10",
////                  "/user/worker11", "/user/worker12"
//            };
//            Workers = new []
//            {
//                Sys.ActorOf(Props.Create<Worker>(), "worker1"), Sys.ActorOf(Props.Create<Worker>(), "worker2"),
//                Sys.ActorOf(Props.Create<Worker>(), "worker3"), Sys.ActorOf(Props.Create<Worker>(), "worker4"),
//                Sys.ActorOf(Props.Create<Worker>(), "worker5"), Sys.ActorOf(Props.Create<Worker>(), "worker6"),
//                Sys.ActorOf(Props.Create<Worker>(), "worker7"), Sys.ActorOf(Props.Create<Worker>(), "worker8"),
//                Sys.ActorOf(Props.Create<Worker>(), "worker9"), Sys.ActorOf(Props.Create<Worker>(), "worker10"),
////                Sys.ActorOf(Props.Create<Worker>(), "worker11"), Sys.ActorOf(Props.Create<Worker>(), "worker12")
//            };
//            Router = Sys.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
//            Workers.ForEach(worker => worker.Tell(new LocalSerivcePack(_servicePack)));
//            Requestor = Sys.ActorOf(AElf.Kernel.Concurrency.Execution.Requestor.Props(Router));
//            _parallelTransactionExecutingService = new ParallelTransactionExecutingService(Requestor, new Grouper(_servicePack.ResourceDetectionService, logger));
//            
//            //set time to maxvalue to run large truck of tx list
//            _parallelTransactionExecutingService.TimeoutMilliSeconds = int.MaxValue;
            
            //set time to maxvalue to run large truck of tx list
            //_parallelTransactionExecutingService.TimeoutMilliSeconds = int.MaxValue;
            
            _dataGenerater = new TransactionDataGenerator(maxTxNum);
            byte[] code = null;
            #if DEBUG
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("./bin/Debug/netcoreapp2.0/AElf.Benchmark.dll")))
            {
                code = file.ReadFully();
            }
            #else
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("./bin/Release/netcoreapp2.0/AElf.Benchmark.dll")))
            {
                code = file.ReadFully();
            }
            #endif
            _contractHash = Prepare(code).Result;
            
            InitContract(_contractHash, _dataGenerater.KeyDict.Keys).GetResult();
            
        }

        private Hash ChainId { get; }
        private int _incrementId = 0;
        

        public static readonly string TestContractZeroName = "AElf.Kernel.Tests.TestContractZero";
        
        public static string TestContractZeroFolder
        {
            get
            {
#if DEBUG
                return $"../{TestContractZeroName}/bin/Debug/netstandard2.0";
    #else
                return $"../{TestContractZeroName}/bin/Release/netstandard2.0";
#endif
                
            }
        }
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath($"{TestContractZeroFolder}/{TestContractZeroName}.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }


        public async Task<KeyValuePair<string, double>> MultipleGroupBenchmark(int txNumber, int groupCount)
        {
            //prepare data
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Benchmark with multiple conflict group");
            Console.WriteLine("-------------------------------------");

            int repeatTime = 20;
        
            var txList = _dataGenerater.GetMultipleGroupTx(txNumber, groupCount, _contractHash);
            long timeused = 0;
            for (int i = 0; i < repeatTime; i++)
            {
                foreach (var tx in txList)
                {
                    tx.IncrementId += 1;
                }
                Stopwatch swExec = new Stopwatch();
                swExec.Start();

                var txResult = await Task.Factory.StartNew(async () =>
                {
                    return await _concurrencyExecutingService.ExecuteAsync(txList, ChainId,new Grouper(_servicePack.ResourceDetectionService, _logger) );
                }).Unwrap();
        
                swExec.Stop();
                timeused += swExec.ElapsedMilliseconds;
                long total = 0;
                txResult.ForEach(result =>
                {
                    if (!result.StdErr.IsNullOrEmpty())
                    {
                        Console.WriteLine(result.StdErr);
                    }
                } );
                string timeStr = string.Join(", ", txResult.Select(a => a.Elapsed.ToString()));
                _logger.Info("Elapsed of every contract: " + timeStr);
                txResult.ForEach(trace =>
                {
                    if (!trace.StdErr.IsNullOrEmpty())
                    {
                        _logger.Error("Execution error: " + trace.StdErr);
                    }
                });
            }
            
            var time = txNumber / (timeused / 1000.0 / (double)repeatTime);
            var str = groupCount + " groups with " + txList.Count + " tx in total";

            return new KeyValuePair<string,double>(str, time);
        }
        
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public async Task<Hash> Prepare(byte[] contractCode)
        {
            //create smart contact zero
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            
            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            var contractAddressZero = _chainCreationService.GenesisContractHash(ChainId);
            
            
            //deploy token contract
            var code = contractCode;
            
            var txnDep = new Transaction()
            {
                From = Hash.Zero.ToAccount(),
                To = contractAddressZero,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, code))
            };
            
            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };
            
            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply(true);

            
            var contractAddr = txnCtxt.Trace.RetVal.DeserializeToPbMessage<Hash>();
            return contractAddr;
        }

        public async Task InitContract(Hash contractAddr, IEnumerable<Hash> addrBook)
        {
            //init contract
            string name = "TestToken" + _incrementId;
            var txnInit = new Transaction
            {
                From = Hash.Zero.ToAccount(),
                To = contractAddr,
                IncrementId = NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(name, Hash.Zero.ToAccount()))
            };
            
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(contractAddr, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply(true);
            
            //init contract
            var initTxList = new List<ITransaction>();
            foreach (var addr in addrBook)
            {
                var txnBalInit = new Transaction
                {
                    From = Hash.Zero.ToAccount(),
                    To = contractAddr,
                    IncrementId = NewIncrementId(),
                    MethodName = "InitBalance",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(addr))
                };
                
                initTxList.Add(txnBalInit);
            }
            var txTrace = await _concurrencyExecutingService.ExecuteAsync(initTxList, ChainId,new Grouper(_servicePack.ResourceDetectionService, _logger));
            ;
        }

        public double BenchmarkGrouping(int txNumber, List<ITransaction> txList)
        {
            byte[] code = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("./bin/Debug/netcoreapp2.0/AElf.Benchmark.dll")))
            {
                code = file.ReadFully();
            }

            var contractHash = Prepare(code).Result;
            
            Grouper grouper = new Grouper(_servicePack.ResourceDetectionService);
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

            grouper.Process(ChainId, txList);
            
            sw.Stop();
            return txNumber / (sw.ElapsedMilliseconds / 1000.0);
        }
        
    }
}