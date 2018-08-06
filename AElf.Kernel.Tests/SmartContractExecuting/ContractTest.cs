using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Types.CSharp;

namespace AElf.Kernel.Tests.SmartContractExecuting
{
    [UseAutofacTestFramework]
    public class ContractTest
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;

        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        private IWorldStateDictator _worldStateDictator;
        private IChainCreationService _chainCreationService;
        private IChainContextService _chainContextService;
        private IBlockManager _blockManager;
        private ITransactionManager _transactionManager;
        private ISmartContractManager _smartContractManager;
        private ISmartContractService _smartContractService;
        private IFunctionMetadataService _functionMetadataService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        private Hash ChainId { get; } = Hash.Generate();

        public ContractTest(IWorldStateDictator worldStateDictator,
            IChainCreationService chainCreationService, IBlockManager blockManager,
            ITransactionManager transactionManager, ISmartContractManager smartContractManager,
            IChainContextService chainContextService, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _worldStateDictator = worldStateDictator;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _smartContractManager = smartContractManager;
            _chainContextService = chainContextService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateDictator, _functionMetadataService);
        }

        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }

        public byte[] ExampleContractCode
        {
            get
            {
                return ContractCodes.TestContractCode;
            }
        }

        [Fact]
        public async Task SmartContractZeroByCreation()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);

            var contractAddressZero = new Hash(ChainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();
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

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);

            var code = ExampleContractCode;
            var contractAddressZero = new Hash(ChainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();

            var txnDep = new Transaction()
            {
                From = Hash.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, code))
            };

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply(true);
            
            Assert.True(string.IsNullOrEmpty(txnCtxt.Trace.StdErr));
            
            var address = txnCtxt.Trace.RetVal.Data.DeserializeToBytes();

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
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
                ContractHash = Hash.Zero
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);

            var code = ExampleContractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };

            var contractAddressZero = new Hash(ChainId.CalculateHashWith(Globals.SmartContractZeroIdString)).ToAccount();

            var txnDep = new Transaction()
            {
                From = Hash.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, code))
            };

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply(true);

            var bs = txnCtxt.Trace.RetVal;
            var address = bs.Data.DeserializeToBytes();

            #region initialize account balance
            var account = Hash.Generate();
            var txnInit = new Transaction
            {
                From = Hash.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account, (ulong)101))
            };
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(address, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply(true);
            
            #endregion initialize account balance

            #region check account balance
            var txnBal = new Transaction
            {
                From = Hash.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
            var txnBalCtxt = new TransactionContext()
            {
                Transaction = txnBal
            };
            await executiveUser.SetTransactionContext(txnBalCtxt).Apply(true);

            Assert.Equal((ulong)101, txnBalCtxt.Trace.RetVal.Data.DeserializeToUInt64());
            #endregion
            
            
            #region check account balance
            var txnPrint = new Transaction
            {
                From = Hash.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "Print"
            };
            
            var txnPrintcxt = new TransactionContext()
            {
                Transaction = txnBal
            };
            await executiveUser.SetTransactionContext(txnPrintcxt).Apply(true);

            //Assert.Equal((ulong)101, txnBalCtxt.Trace.RetVal.DeserializeToUInt64());
            #endregion
        }
    }
}