using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Metadata;
using Xunit.Frameworks.Autofac;
using Path = AElf.Kernel.Path;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Types.CSharp;
using Akka.Actor;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    public class MockSetup
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
        public IActorRef Router { get;  }
        public IActorRef Worker1 { get; }
        public IActorRef Worker2 { get; }
        public IActorRef Requestor { get; }

        public Hash ChainId1 { get; } = Hash.Generate();
        public Hash ChainId2 { get; } = Hash.Generate();
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;

        public IChainContextService ChainContextService;

        public IAccountDataProvider DataProvider1;
        public IAccountDataProvider DataProvider2;

        public Hash SampleContractAddress1 { get; } = Hash.Generate();
        public Hash SampleContractAddress2 { get; } = Hash.Generate();

        public IExecutive Executive1 { get; private set; }
        public IExecutive Executive2 { get; private set; }

        public ServicePack ServicePack;

        private IWorldStateDictator _worldStateDictator;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;
        private IFunctionMetadataService _functionMetadataService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IWorldStateStore worldStateStore, IChangesStore changesStore, IDataStore dataStore, IChainCreationService chainCreationService,
            IBlockManager blockManager, SmartContractStore smartContractStore, IChainContextService chainContextService, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _worldStateDictator = new WorldStateDictator( worldStateStore, changesStore,  dataStore);
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            SmartContractManager = new SmartContractManager(smartContractStore);
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService =
                new SmartContractService(SmartContractManager, _smartContractRunnerFactory, _worldStateDictator, functionMetadataService);
            Task.Factory.StartNew(async () => { await DeploySampleContracts(); }).Unwrap().Wait();
            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = new NewMockResourceUsageDetectionService(),
                WorldStateDictator = _worldStateDictator
            };
            
            var workers = new[] {"/user/worker1", "/user/worker2"};
            Worker1 = Sys.ActorOf(Props.Create<Worker>(), "worker1");
            Worker2 = Sys.ActorOf(Props.Create<Worker>(), "worker2");
            Router = Sys.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
            Worker1.Tell(new LocalSerivcePack(ServicePack));
            Worker2.Tell(new LocalSerivcePack(ServicePack));
            Requestor = Sys.ActorOf(AElf.Kernel.Concurrency.Execution.Requestor.Props(Router));
        }
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, reg);
            var genesis1 = await _blockManager.GetBlockAsync(chain1.GenesisBlockHash);
            DataProvider1 =
                await (_worldStateDictator.SetChainId(ChainId1)).GetAccountDataProvider(
                    Path.CalculatePointerForAccountZero(ChainId1));

            var chain2 = await _chainCreationService.CreateNewChainAsync(ChainId2, reg);
            var genesis2 = await _blockManager.GetBlockAsync(chain2.GenesisBlockHash);
            DataProvider2 =
                await (_worldStateDictator.SetChainId(ChainId2)).GetAccountDataProvider(
                    Path.CalculatePointerForAccountZero(ChainId2));
        }

        private async Task DeploySampleContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(ExampleContractCode),
                ContractHash = new Hash(ExampleContractCode)
            };

            await SmartContractService.DeployContractAsync(ChainId1, SampleContractAddress1, reg);
            await SmartContractService.DeployContractAsync(ChainId2, SampleContractAddress2, reg);
            Executive1 = await SmartContractService.GetExecutiveAsync(SampleContractAddress1, ChainId1);
            Executive2 = await SmartContractService.GetExecutiveAsync(SampleContractAddress2, ChainId2);
        }

        public byte[] ExampleContractCode
        {
            get { return ContractCodes.TestContractCode; }
        }

        public void Initialize1(Hash account, ulong qty)
        {
            Executive1.SetTransactionContext(GetInitializeTxnCtxt(SampleContractAddress1, account, qty)).Apply(true)
                .Wait();
        }

        public void Initialize2(Hash account, ulong qty)
        {
            Executive2.SetTransactionContext(GetInitializeTxnCtxt(SampleContractAddress2, account, qty)).Apply(true)
                .Wait();
        }

        private TransactionContext GetInitializeTxnCtxt(Hash contractAddress, Hash account, ulong qty)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account, qty))
            };
            return new TransactionContext()
            {
                Transaction = tx
            };
        }

        public Transaction GetTransferTxn1(Hash from, Hash to, ulong qty)
        {
            return GetTransferTxn(SampleContractAddress1, from, to, qty);
        }

        public Transaction GetTransferTxn2(Hash from, Hash to, ulong qty)
        {
            return GetTransferTxn(SampleContractAddress2, from, to, qty);
        }

        public Transaction GetSleepTxn1(int milliSeconds)
        {
            return GetSleepTxn(SampleContractAddress1, milliSeconds);
        }

        private Transaction GetSleepTxn(Hash contractAddress, int milliSeconds)
        {
            return new Transaction
            {
                From = Hash.Zero,
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

        private Transaction GetNoActionTxn(Hash contractAddress)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "NoAction",
                Params = ByteString.Empty
            };
        }


        private Transaction GetTransferTxn(Hash contractAddress, Hash from, Hash to, ulong qty)
        {
            // TODO: Test with IncrementId
            return new Transaction
            {
                From = from,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, qty))
            };
        }

        private Transaction GetBalanceTxn(Hash contractAddress, Hash account)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
        }

        public ulong GetBalance1(Hash account)
        {
            var txn = GetBalanceTxn(SampleContractAddress1, account);
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };

            Executive1.SetTransactionContext(txnCtxt).Apply(true).Wait();

            return txnCtxt.Trace.RetVal.DeserializeToUInt64();
        }

        public ulong GetBalance2(Hash account)
        {
            var txn = GetBalanceTxn(SampleContractAddress2, account);
            var txnRes = new TransactionResult();
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };
            Executive2.SetTransactionContext(txnCtxt).Apply(true).Wait();

            return txnCtxt.Trace.RetVal.DeserializeToUInt64();
        }

        private Transaction GetSTTxn(Hash contractAddress, Hash transactionHash)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                MethodName = "GetTransactionStartTime",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(transactionHash))
            };
        }

        private Transaction GetETTxn(Hash contractAddress, Hash transactionHash)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                MethodName = "GetTransactionEndTime",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(transactionHash))
            };
        }

        public DateTime GetTransactionStartTime1(ITransaction tx)
        {
            var txn = GetSTTxn(SampleContractAddress1, tx.GetHash());
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };

            Executive1.SetTransactionContext(txnCtxt).Apply(true).Wait();

            var dtStr = txnCtxt.Trace.RetVal.DeserializeToString();
            //var dtStr = BitConverter.ToString(txnCtxt.Trace.RetVal.Unpack<BytesValue>().Value.ToByteArray()).Replace("-", "");

            return DateTime.ParseExact(dtStr, "yyyy-MM-dd HH:mm:ss.ffffff", null);
        }

        public DateTime GetTransactionEndTime1(ITransaction tx)
        {
            var txn = GetETTxn(SampleContractAddress1, tx.GetHash());
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };

            Executive1.SetTransactionContext(txnCtxt).Apply(true).Wait();

            var dtStr = txnCtxt.Trace.RetVal.DeserializeToString();

            return DateTime.ParseExact(dtStr, "yyyy-MM-dd HH:mm:ss.ffffff", null);
        }
    }
}