//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using AElf.ChainController;
//using AElf.SmartContract;
//using AElf.Kernel.Managers;
//using Google.Protobuf;
//using Xunit;
//using AElf.Types.CSharp;
//using Google.Protobuf.WellKnownTypes;
//using AElf.Common;
//using AElf.Kernel.SmartContract.Domain;
//using AElf.Kernel.SmartContractExecution.Domain;
//using AElf.Kernel.Types;
//using AElf.TestBase;

namespace AElf.Kernel.Tests.SmartContractExecuting
{
    /*
    public sealed class ContractTest : AElfKernelTestBase
    {
        // todo warning this test obviously uses bad  
        
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId;

        private ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        private IChainCreationService _chainCreationService;
        private IStateManager _stateManager;
        private ISmartContractManager _smartContractManager;
        private ISmartContractService _smartContractService;

        public ContractTest()
        {
            _stateManager = GetRequiredService<IStateManager>();
            _chainCreationService = GetRequiredService<IChainCreationService>();
            _smartContractManager = GetRequiredService<ISmartContractManager>();
            _smartContractService = GetRequiredService<ISmartContractService>();
        }

        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        private byte[] ExampleContractCode => ContractCodes.TestContractCode;

        [Fact]
        public async Task SmartContractZeroByCreation()
        {
            int ChainId = ChainHelpers.GetRandomChainId();
        
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Empty
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, new List<SmartContractRegistration>{reg});
           
            var contractAddressZero = ContractHelpers.GetSystemContractAddress(ChainId, GlobalConfig.GenesisBasicContract);
            var copy = await _smartContractManager.GetAsync(reg.ContractHash);

            // throw exception if not registered
            Assert.Equal(reg, copy);
        }

        [Fact]
        public async Task DeployUserContract()
        {
            int ChainId = ChainHelpers.GetRandomChainId();
            
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Empty
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, new List<SmartContractRegistration>{reg});
            
            var code = ExampleContractCode;
            var contractAddressZero = ContractHelpers.GetSystemContractAddress(ChainId, GlobalConfig.GenesisBasicContract);

            var txnDep = new Transaction()
            {
                From = Address.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, code))
            };

            var txnCtxt = new TransactionContext
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();
            await txnCtxt.Trace.SmartCommitChangesAsync(_stateManager);
            
            Assert.True(string.IsNullOrEmpty(txnCtxt.Trace.StdErr));
            
            var address = Address.FromBytes(txnCtxt.Trace.RetVal.Data.DeserializeToBytes());

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = Hash.FromRawBytes(code)
            };
            var copy = await _smartContractService.GetContractByAddressAsync(ChainId, address);
            
            Assert.Equal(regExample.ContractHash, copy.ContractHash);
            Assert.Equal(regExample.ContractBytes, copy.ContractBytes);
        }

        [Fact]
        public async Task Invoke()
        {
            int ChainId = ChainHelpers.GetRandomChainId();
            
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Empty,
                SerialNumber = GlobalConfig.GenesisBasicContract
            };

            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, new List<SmartContractRegistration>{reg});

            var code = ExampleContractCode;

            var contractAddressZero = ContractHelpers.GetSystemContractAddress(ChainId, GlobalConfig.GenesisBasicContract);

            var txnDep = new Transaction()
            {
                From = Address.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, code))
            };

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();
            await txnCtxt.Trace.SmartCommitChangesAsync(_stateManager);

            var returnVal = txnCtxt.Trace.RetVal;
            var address = Address.FromBytes(returnVal.Data.DeserializeToBytes());

            #region initialize account balance
            var account = Address.Generate();
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
            await txnInitCtxt.Trace.SmartCommitChangesAsync(_stateManager);
            
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
            await txnPrintcxt.Trace.SmartCommitChangesAsync(_stateManager);

            //Assert.Equal((ulong)101, txnBalCtxt.Trace.RetVal.DeserializeToUInt64());
            #endregion
        }
    }
    */
}