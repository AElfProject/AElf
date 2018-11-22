using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Managers;
using Google.Protobuf;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Execution;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Types.CSharp;
using Akka.Actor;
using Google.Protobuf.WellKnownTypes;
using Mono.Cecil.Cil;
using NLog;
using AElf.Common;
using AElf.Execution.Execution;
using Address = AElf.Common.Address;

namespace AElf.Kernel.Tests
{
    public class BlockChainTests_MockSetup
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public Hash ChainId1 { get; } = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 });
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;

        public IChainContextService ChainContextService;

        public Address SampleContractAddress1 { get; } = Address.FromString("SampleContractAddress1");

        public IExecutive Executive1 { get; private set; }

        private IChainCreationService _chainCreationService;
        public IChainService ChainService { get; }

        public IBlockChain BlockChain => ChainService.GetBlockChain(ChainId1);

        private IFunctionMetadataService _functionMetadataService;
        private ILogger _logger;

        private IStateStore _stateStore;
        public IActorEnvironment ActorEnvironment { get; private set; }

        private readonly HashManager _hashManager;
        private readonly TransactionManager _transactionManager;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public BlockChainTests_MockSetup(IDataStore dataStore, IChainCreationService chainCreationService,
            IChainService chainService,
            IChainContextService chainContextService, IFunctionMetadataService functionMetadataService,
            ISmartContractRunnerFactory smartContractRunnerFactory, ILogger logger,
            IStateStore stateStore, HashManager hashManager, TransactionManager transactionManager)
        {
            _logger = logger;
            _stateStore = stateStore;
            _hashManager = hashManager;
            _transactionManager = transactionManager;
            _chainCreationService = chainCreationService;
            ChainService = chainService;
            ChainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            SmartContractManager = new SmartContractManager(dataStore);
            Task.Factory.StartNew(async () => { await Init(); }).Unwrap().Wait();
            SmartContractService =
                new SmartContractService(SmartContractManager, _smartContractRunnerFactory, stateStore,
                    functionMetadataService);
            Task.Factory.StartNew(async () => { await DeploySampleContracts(); }).Unwrap().Wait();
        }

        public byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero,
                Type = (int) SmartContractType.BasicContractZero
            };
            await _chainCreationService.CreateNewChainAsync(ChainId1,  new List<SmartContractRegistration>{reg});
        }

        private async Task DeploySampleContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFrom(ExampleContractCode),
                ContractHash = Hash.FromRawBytes(ExampleContractCode)
            };

            await SmartContractService.DeployContractAsync(ChainId1, SampleContractAddress1, reg, true);
            Executive1 = await SmartContractService.GetExecutiveAsync(SampleContractAddress1, ChainId1);
        }

        public byte[] ExampleContractCode => ContractCodes.TestContractCode;

        public async Task CommitTrace(TransactionTrace trace)
        {
            await trace.CommitChangesAsync(_stateStore);
        }

        public void Initialize1(Address account, ulong qty)
        {
            var tc = GetInitializeTxnCtxt(SampleContractAddress1, account, qty);
            Executive1.SetTransactionContext(tc).Apply()
                .Wait();
            CommitTrace(tc.Trace).Wait();
        }

        public Transaction GetInitializeTxn(Address account, ulong qty)
        {
            var tx = new Transaction
            {
                From = Address.Zero,
                To = SampleContractAddress1,
                IncrementId = NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account, new UInt64Value {Value = qty}))
            };
            return tx;
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
            return GetTransferTxn(from, to, qty);
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


        private Transaction GetTransferTxn(Address from, Address to, ulong qty)
        {
            // TODO: Test with IncrementId
            return new Transaction
            {
                From = from,
                To = SampleContractAddress1,
                IncrementId = NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, new UInt64Value {Value = qty}))
            };
        }

        public Transaction GetBalanceTxn(Address account)
        {
            return new Transaction
            {
                From = Address.Zero,
                To = SampleContractAddress1,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
        }

        public ulong GetBalance(Address account)
        {
            var txn = GetBalanceTxn(account);
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn,
                Trace = new TransactionTrace()
            };
            var t = SmartContractService.GetExecutiveAsync(SampleContractAddress1, ChainId1);
            t.Wait();
            t.Result.SetTransactionContext(txnCtxt).Apply().Wait();
            return txnCtxt.Trace.RetVal.Data.DeserializeToUInt64();
        }

    }
}