//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.Kernel.Managers;
//using Google.Protobuf;
//using AElf.ChainController;
//using AElf.SmartContract;
//using AElf.Kernel.SmartContractExecution;
//using AElf.Kernel.Core.Tests.Concurrency.Scheduling;
//using AElf.CSharp.Core;
//using Akka.Actor;
//using Google.Protobuf.WellKnownTypes;
//using Mono.Cecil.Cil;
//using AElf.Types;
//using AElf.Kernel.SmartContractExecution.Execution;
//using AElf.Kernel.Blockchain.Domain;
//using AElf.Kernel.SmartContract.Domain;
//using AElf.Kernel.SmartContractExecution.Application;
//using AElf.Kernel.SmartContractExecution.Domain;
//using AElf.SmartContract.Contexts;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Volo.Abp.DependencyInjection;
//using Address = AElf.Common.Address;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    /*
    public class MockSetup : ISingletonDependency
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public ActorSystem Sys { get; } = ActorSystem.Create("test");
        public IActorRef Router { get; }
        public IActorRef Worker1 { get; }
        public IActorRef Worker2 { get; }
        public IActorRef Requestor { get; }

        public int ChainId1 { get; } = ChainHelpers.GetChainId(101);
        public int ChainId2 { get; } = ChainHelpers.GetChainId(102);
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;

        public IChainContextService ChainContextService;

        public IAccountDataProvider DataProvider1;
        public IAccountDataProvider DataProvider2;

        public Address SampleContractAddress1;
        public Address SampleContractAddress2;

        public IExecutive Executive1 { get; private set; }
        public IExecutive Executive2 { get; private set; }

        public ServicePack ServicePack;

        private IChainCreationService _chainCreationService;
        public ILogger<MockSetup> Logger { get; set; }

        private IStateManager _stateManager;
        public IActorEnvironment ActorEnvironment { get; private set; }

        public MockSetup(IChainCreationService chainCreationService,
            IChainService chainService, IActorEnvironment actorEnvironment,
            IChainContextService chainContextService, IFunctionMetadataService functionMetadataService,
            IStateProviderFactory stateProviderFactory, TransactionManager transactionManager,
            ISmartContractManager smartContractManager, ISmartContractRunnerContainer smartContractRunnerContainer,
            ISmartContractService smartContractService)
        {
            Logger = NullLogger<MockSetup>.Instance;
            _stateManager = stateProviderFactory.CreateStateManager();
            ActorEnvironment = actorEnvironment;
            if (!ActorEnvironment.Initialized)
            {
                ActorEnvironment.InitActorSystem();
            }

            _chainCreationService = chainCreationService;
            ChainContextService = chainContextService;
            SmartContractManager = smartContractManager;
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService = smartContractService;
            Task.Factory.StartNew(async () => { await DeploySampleContracts(); }).Unwrap().Wait();
            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = new NewMockResourceUsageDetectionService(),
                StateManager = _stateManager
            };

            // These are only required for workertest
            // other tests use ActorEnvironment
            var workers = new[] {"/user/worker1", "/user/worker2"};
            Worker1 = Sys.ActorOf(Props.Create<Worker>(), "worker1");
            Worker2 = Sys.ActorOf(Props.Create<Worker>(), "worker2");
            Router = Sys.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
            Worker1.Tell(new LocalSerivcePack(ServicePack));
            Worker2.Tell(new LocalSerivcePack(ServicePack));
            Requestor = Sys.ActorOf(SmartContractExecution.Execution.Requestor.Props(Router));
        }

        public byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode),
                SerialNumber = GlobalConfig.GenesisBasicContract
            };
            var chain1 =
                await _chainCreationService.CreateNewChainAsync(ChainId1, new List<SmartContractRegistration> {reg});
            var chain2 =
                await _chainCreationService.CreateNewChainAsync(ChainId2, new List<SmartContractRegistration> {reg});
        }

        private async Task DeploySampleContracts()
        {
            const ulong serialNumber = 10ul;
            SampleContractAddress1 = Address.BuildContractAddress(ChainId1, serialNumber);
            SampleContractAddress2 = Address.BuildContractAddress(ChainId2, serialNumber);

            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFrom(ExampleContractCode),
                ContractHash = Hash.FromRawBytes(ExampleContractCode),
                SerialNumber = serialNumber
            };

            await SmartContractService.DeploySystemContractAsync(ChainId1, reg);
            await SmartContractService.DeploySystemContractAsync(ChainId2, reg);
            Executive1 = await SmartContractService.GetExecutiveAsync(SampleContractAddress1, ChainId1);
            Executive2 = await SmartContractService.GetExecutiveAsync(SampleContractAddress2, ChainId2);
        }

        public byte[] ExampleContractCode => ContractCodes.TestContractCode;

        public async Task CommitTrace(TransactionTrace trace)
        {
            await trace.SmartCommitChangesAsync(_stateManager);
//            await StateDictator.ApplyCachedDataAction(changesDict);
        }

        public void Initialize1(Address account, ulong qty)
        {
            var tc = GetInitializeTxnCtxt(SampleContractAddress1, account, qty);
            Executive1.SetTransactionContext(tc).Apply()
                .Wait();
            CommitTrace(tc.Trace).Wait();
        }

        public void Initialize2(Address account, ulong qty)
        {
            var tc = GetInitializeTxnCtxt(SampleContractAddress2, account, qty);
            Executive2.SetTransactionContext(tc).Apply()
                .Wait();
            CommitTrace(tc.Trace).Wait();
        }

        private TransactionContext GetInitializeTxnCtxt(Address contractAddress, Address account, ulong qty)
        {
            var tx = new Transaction
            {
                From = Address.Zero,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account, new UInt64Value {Value = qty}))
            };
            return new TransactionContext()
            {
                Transaction = tx
            };
        }

        public Transaction GetTransferTxn1(Address from, Address to, ulong qty)
        {
            return GetTransferTxn(SampleContractAddress1, from, to, qty);
        }

        public Transaction GetTransferTxn2(Address from, Address to, ulong qty)
        {
            return GetTransferTxn(SampleContractAddress2, from, to, qty);
        }

        public Transaction GetSleepTxn1(int milliSeconds)
        {
            return GetSleepTxn(SampleContractAddress1, milliSeconds);
        }

        private Transaction GetSleepTxn(Address contractAddress, int milliSeconds)
        {
            return new Transaction
            {
                From = Address.Zero,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "SleepMilliseconds",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(milliSeconds))
            };
        }

        public Transaction GetNoActionTxn1()
        {
            return GetNoActionTxn(SampleContractAddress1);
        }

        private Transaction GetNoActionTxn(Address contractAddress)
        {
            return new Transaction
            {
                From = Address.Zero,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "NoAction",
                Params = ByteString.Empty
            };
        }


        private Transaction GetTransferTxn(Address contractAddress, Address from, Address to, ulong qty)
        {
            // TODO: Test with IncrementId
            return new Transaction
            {
                From = from,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, new UInt64Value {Value = qty}))
            };
        }

        public Transaction GetBalanceTxn(Address contractAddress, Address account)
        {
            return new Transaction
            {
                From = Address.Zero,
                To = contractAddress,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
        }

        public ulong GetBalance1(Address account)
        {
            var txn = GetBalanceTxn(SampleContractAddress1, account);
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn,
                Trace = new TransactionTrace()
            };
            // TODO: Check why this doesn't work
//            Executive1.SetDataCache(new Dictionary<DataPath, StateCache>());
//            Executive1.SetTransactionContext(txnCtxt).Apply().Wait();
            var t = SmartContractService.GetExecutiveAsync(SampleContractAddress1, ChainId1);
            t.Wait();
            t.Result.SetTransactionContext(txnCtxt).Apply().Wait();

            return txnCtxt.Trace.RetVal.Data.DeserializeToUInt64();
        }

        public ulong GetBalance2(Address account)
        {
            var txn = GetBalanceTxn(SampleContractAddress2, account);
            var txnRes = new TransactionResult();
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };
            Executive2.SetDataCache(new Dictionary<StatePath, StateCache>());
            Executive2.SetTransactionContext(txnCtxt).Apply().Wait();

            return txnCtxt.Trace.RetVal.Data.DeserializeToUInt64();
        }

        private Transaction GetSTTxn(Address contractAddress, Hash transactionId)
        {
            return new Transaction
            {
                From = Address.Zero,
                To = contractAddress,
                MethodName = "GetTransactionStartTime",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(transactionId))
            };
        }

        private Transaction GetETTxn(Address contractAddress, Hash transactionId)
        {
            return new Transaction
            {
                From = Address.Zero,
                To = contractAddress,
                MethodName = "GetTransactionEndTime",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(transactionId))
            };
        }

        public DateTime GetTransactionStartTime1(Transaction tx)
        {
            var txn = GetSTTxn(SampleContractAddress1, tx.GetHash());
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };
            Executive1.SetDataCache(new Dictionary<StatePath, StateCache>());
            Executive1.SetTransactionContext(txnCtxt).Apply().Wait();

            var dtStr = txnCtxt.Trace.RetVal.Data.DeserializeToString();

            return DateTime.ParseExact(dtStr, "yyyy-MM-dd HH:mm:ss.ffffff", null);
        }

        public DateTime GetTransactionEndTime1(Transaction tx)
        {
            var txn = GetETTxn(SampleContractAddress1, tx.GetHash());
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };
            Executive1.SetDataCache(new Dictionary<StatePath, StateCache>());
            Executive1.SetTransactionContext(txnCtxt).Apply().Wait();

            var dtStr = txnCtxt.Trace.RetVal.Data.DeserializeToString();

            return DateTime.ParseExact(dtStr, "yyyy-MM-dd HH:mm:ss.ffffff", null);
        }
    }
    */
}