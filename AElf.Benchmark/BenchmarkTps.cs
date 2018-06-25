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
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
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
        private readonly IParallelTransactionExecutingService _parallelTransactionExecutingService;
        private readonly IWorldStateManager _worldStateManager;
        private readonly IAccountContextService _accountContextService;
        
        public ActorSystem Sys { get; } = ActorSystem.Create("benchmark");
        public IActorRef Router { get;  }
        public IActorRef[] Workers { get; }
        public IActorRef Requestor { get; }

        private readonly ServicePack _servicePack;

        private readonly TransactionDataGenerator _dataGenerater;
        private readonly Hash _contractHash;

        public Benchmarks(IChainCreationService chainCreationService, IBlockManager blockManager, IChainContextService chainContextService, int maxTxNum, ISmartContractRunnerFactory smartContractRunnerFactory, ISmartContractService smartContractService, ILogger logger, IFunctionMetadataService functionMetadataService, IWorldStateManager worldStateManager, IAccountContextService accountContextService)
        {
            ChainId = Hash.Generate();
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractService = smartContractService;
            _worldStateManager = worldStateManager;
            _accountContextService = accountContextService;

            _worldStateManager.OfChain(ChainId);

            _servicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = _smartContractService,
                ResourceDetectionService = new ResourceUsageDetectionService(functionMetadataService),
                WorldStateManager = _worldStateManager,
                AccountContextService = _accountContextService
            };

            var workers = new[]
            {
                "/user/worker1", "/user/worker2"
                //, "/user/worker3", "/user/worker4", "/user/worker5", "/user/worker6",
                //"/user/worker7", "/user/worker8", "/user/worker9", "/user/worker10", "/user/worker11", "/user/worker12"
            };
            Workers = new []
            {
                Sys.ActorOf(Props.Create<Worker>(), "worker1"), Sys.ActorOf(Props.Create<Worker>(), "worker2"),
                /*Sys.ActorOf(Props.Create<Worker>(), "worker3"), Sys.ActorOf(Props.Create<Worker>(), "worker4"),
                Sys.ActorOf(Props.Create<Worker>(), "worker5"), Sys.ActorOf(Props.Create<Worker>(), "worker6"),
                Sys.ActorOf(Props.Create<Worker>(), "worker7"), Sys.ActorOf(Props.Create<Worker>(), "worker8"),
                Sys.ActorOf(Props.Create<Worker>(), "worker9"), Sys.ActorOf(Props.Create<Worker>(), "worker10"),
                Sys.ActorOf(Props.Create<Worker>(), "worker11"), Sys.ActorOf(Props.Create<Worker>(), "worker12")*/
            };
            Router = Sys.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
            Workers.ForEach(worker => worker.Tell(new LocalSerivcePack(_servicePack)));
            Requestor = Sys.ActorOf(AElf.Kernel.Concurrency.Execution.Requestor.Props(Router));
            _parallelTransactionExecutingService = new ParallelTransactionExecutingService(Requestor, new Grouper(_servicePack.ResourceDetectionService, logger));
            
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

        public async Task<Dictionary<string, double>> SingleGroupBenchmark(int txNumber, double conflictRate)
        {
            var txList = _dataGenerater.GetTxsWithOneConflictGroup(_contractHash, txNumber, conflictRate);
            //Console.WriteLine("start to check signature");
            Stopwatch swVerifer = new Stopwatch();
            swVerifer.Start();

            foreach (var tx in txList)
            {
                ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(tx.P.ToByteArray());
                ECVerifier verifier = new ECVerifier(recipientKeyPair);
                if(!verifier.Verify(tx.GetSignature(), tx.GetHash().GetHashBytes()))
                {
                    throw new Exception("Signature failed");
                }
            }
            
            swVerifer.Stop();
            
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Benchmark with single conflict group");
            Console.WriteLine("-------------------------------------");
            //Execution
            Stopwatch swExec = new Stopwatch();
            swExec.Start();

            
            var txResult = Task.Factory.StartNew(async () =>
            {
                return await _parallelTransactionExecutingService.ExecuteAsync(txList, ChainId);
            }).Unwrap().Result;
            
            swExec.Stop();
            /*
            var dataProvider = (await _worldStateManager.OfChain(ChainId)).GetAccountDataProvider(_contractHash).GetDataProvider();
            _smartContractContext.ChainId = ChainId;
            _smartContractContext.DataProvider = dataProvider;
            Api.SetSmartContractContext(_smartContractContext);
            Api.SetTransactionContext(_transactionContext);
            
            TestTokenContract contract = new TestTokenContract();
            await contract.InitializeAsync("token1", Hash.Zero.ToAccount());
            
            Stopwatch swNoReflaction = new Stopwatch();
            swNoReflaction.Start();
            
            foreach (var tx in txList)
            {
                var parameters = AElf.Kernel.Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();
                await contract.Transfer(tx.From, (Hash)parameters[1], 50);
            }
            
            swNoReflaction.Stop();
            */
            Dictionary<string, double> res = new Dictionary<string, double>();

            var verifyPerSec = txNumber / (swVerifer.ElapsedMilliseconds / 1000.0);
            res.Add("verifyTPS", verifyPerSec);

            var executeTPS = txNumber / (swExec.ElapsedMilliseconds / 1000.0);
            res.Add("executeTPS", executeTPS);
            return res;
        }

        public async Task<Dictionary<string, double>> MultipleGroupBenchmark(int txNumber, int maxGroupNumber)
        {
            var res = new Dictionary<string, double>();
            //prepare data
            Console.WriteLine("-------------------------------------");
            Console.WriteLine("Benchmark with multiple conflict group");
            Console.WriteLine("-------------------------------------");
            
            for (int groupCount = 1; groupCount <= maxGroupNumber; groupCount++)
            {
                var txList = _dataGenerater.GetMultipleGroupTx(txNumber, groupCount, _contractHash);
                long timeused = 0;
                for (int i = 0; i < 10; i++)
                {
                    foreach (var tx in txList)
                    {
                        tx.IncrementId += 1;
                    }
                    Stopwatch swExec = new Stopwatch();
                    swExec.Start();

                    var txResult = Task.Factory.StartNew(async () =>
                    {
                        return await _parallelTransactionExecutingService.ExecuteAsync(txList, ChainId);
                    }).Unwrap().Result;
            
                    swExec.Stop();
                    long total = 0;
                    txResult.ForEach(result => total += result.Elapsed);
                    Console.WriteLine("Average elapsed: " + total / txResult.Count);
                    timeused += swExec.ElapsedMilliseconds;
                }
                
                var time = txNumber / (timeused / 1000.0 / 10.0);
                var str = groupCount + " groups with " + txList.Count + " tx in total";
                res.Add(str, time);
                Console.WriteLine(str + ": " + time);
            }

            return res;
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
            var contractAddressZero = new Hash(ChainId.CalculateHashWith("__SmartContractZero__")).ToAccount();
            
            
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
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(name, Hash.Zero.ToAccount()))
            };
            
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(contractAddr, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply(true);
            
            //init contract
            int current = 0;
            foreach (var addr in addrBook)
            {
                current++;
                if (addrBook.Count() > 100 && current % (addrBook.Count() / 10) == 0)
                {
                    Console.WriteLine("Contract Init: " + (double)(current * 100) / (double)addrBook.Count() + "%");
                }
                var txnBalInit = new Transaction
                {
                    From = Hash.Zero.ToAccount(),
                    To = contractAddr,
                    IncrementId = NewIncrementId(),
                    MethodName = "InitBalance",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(addr))
                };
            
                var txnBalInitCtx = new TransactionContext()
                {
                    Transaction = txnBalInit
                };
                var executiveBalInitUser = await _smartContractService.GetExecutiveAsync(contractAddr, ChainId);
                await executiveBalInitUser.SetTransactionContext(txnBalInitCtx).Apply(true);
            }
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