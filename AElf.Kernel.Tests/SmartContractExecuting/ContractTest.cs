using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Types.CSharp;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Tests.SmartContractExecuting
{
    [UseAutofacTestFramework]
    public class ContractTest
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId;

        private ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        private IChainCreationService _chainCreationService;
        private IChainContextService _chainContextService;
        private IChainService _chainService;
        private ITransactionManager _transactionManager;
        private IStateStore _stateStore;
        private ISmartContractManager _smartContractManager;
        private ISmartContractService _smartContractService;
        private IFunctionMetadataService _functionMetadataService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        private Hash ChainId { get; } = Hash.Generate();

        public ContractTest(IStateStore stateStore,
            IChainCreationService chainCreationService, IChainService chainService,
            ITransactionManager transactionManager, ISmartContractManager smartContractManager,
            IChainContextService chainContextService, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _stateStore = stateStore;
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _transactionManager = transactionManager;
            _smartContractManager = smartContractManager;
            _chainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, stateStore, _functionMetadataService);
        }

        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        private byte[] ExampleContractCode => ContractCodes.TestContractCode;

        [Fact]
        public async Task SmartContractZeroByCreation()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, new List<SmartContractRegistration>{reg});
           
            var contractAddressZero = AddressHelpers.GetSystemContractAddress(ChainId, GlobalConfig.GenesisBasicContract);
            var copy = await _smartContractManager.GetAsync(contractAddressZero);

            // throw exception if not registered
            Assert.Equal(reg, copy);
        }

        [Fact]
        public async Task DeployUserContract()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, new List<SmartContractRegistration>{reg});
            
            var code = ExampleContractCode;
            var contractAddressZero = AddressHelpers.GetSystemContractAddress(ChainId, GlobalConfig.GenesisBasicContract);

            var txnDep = new Transaction()
            {
                From = Address.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, ContractCodes.TestContractName, code))
            };

            var txnCtxt = new TransactionContext
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();
            await txnCtxt.Trace.CommitChangesAsync(_stateStore);
            
            Assert.True(string.IsNullOrEmpty(txnCtxt.Trace.StdErr));
            
            var address = Address.FromRawBytes(txnCtxt.Trace.RetVal.Data.DeserializeToBytes());

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = Hash.FromRawBytes(code)
            };
            var copy = await _smartContractManager.GetAsync(address);

            Assert.Equal(regExample, copy);
        }

        [Fact]
        public async Task Invoke()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero,
                Type = (int)SmartContractType.BasicContractZero
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, new List<SmartContractRegistration>{reg});

            var code = ExampleContractCode;

            var contractAddressZero = AddressHelpers.GetSystemContractAddress(ChainId, GlobalConfig.GenesisBasicContract);

            var txnDep = new Transaction()
            {
                From = Address.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1,ContractCodes.TestContractName, code))
            };

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();
            await txnCtxt.Trace.CommitChangesAsync(_stateStore);

            var bs = txnCtxt.Trace.RetVal;
            var address = Address.FromRawBytes(bs.Data.DeserializeToBytes());

            #region initialize account balance
            var account = Address.FromRawBytes(Hash.Generate().ToByteArray());
            var txnInit = new Transaction
            {
                From = Address.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account, new UInt64Value {Value = 101}))
            };
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(address, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply();
            await txnInitCtxt.Trace.CommitChangesAsync(_stateStore);
            
            #endregion initialize account balance

            #region check account balance
            var txnBal = new Transaction
            {
                From = Address.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
            var txnBalCtxt = new TransactionContext()
            {
                Transaction = txnBal
            };
            await executiveUser.SetTransactionContext(txnBalCtxt).Apply();

            Assert.Equal((ulong)101, txnBalCtxt.Trace.RetVal.Data.DeserializeToUInt64());
            #endregion
            
            #region check account balance
            var txnPrint = new Transaction
            {
                From = Address.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "Print"
            };
            
            var txnPrintcxt = new TransactionContext()
            {
                Transaction = txnBal
            };
            await executiveUser.SetTransactionContext(txnPrintcxt).Apply();
            await txnPrintcxt.Trace.CommitChangesAsync(_stateStore);

            //Assert.Equal((ulong)101, txnBalCtxt.Trace.RetVal.DeserializeToUInt64());
            #endregion
        }
    }
}