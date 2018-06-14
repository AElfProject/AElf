﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;
using Type = System.Type;
using AElf.Runtime.CSharp;
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

        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IChainContextService _chainContextService;
        private IBlockManager _blockManager;
        private ITransactionManager _transactionManager;
        private ISmartContractManager _smartContractManager;
        private ISmartContractService _smartContractService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();

        private Hash ChainId { get; } = Hash.Generate();

        public ContractTest(IWorldStateManager worldStateManager,
            IChainCreationService chainCreationService, IBlockManager blockManager,
            ITransactionManager transactionManager, ISmartContractManager smartContractManager,
            IChainContextService chainContextService)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            _smartContractManager = smartContractManager;
            _chainContextService = chainContextService;
            var runner = new SmartContractRunner(ContractCodes.TestContractFolder);
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager);
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

            var contractAddress = ChainId.CalculateHashWith("__SmartContractZero__");
            var copy = await _smartContractManager.GetAsync(contractAddress);

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

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };

            var contractAddressZero = ChainId.CalculateHashWith("__SmartContractZero__");

            var txnDep = new Transaction()
            {
                From = Hash.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsHolder.Pack(regExample).ToByteArray())
            };

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();

            var address = (Hash)txnCtxt.Trace.RetVal.AnyToPbMessage(typeof(Hash));

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

            var contractAddressZero = ChainId.CalculateHashWith("__SmartContractZero__");

            var txnDep = new Transaction()
            {
                From = Hash.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsHolder.Pack(regExample).ToByteArray())
            };

            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };

            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();

            var address = txnCtxt.Trace.RetVal.Unpack<Hash>();

            #region initialize account balance
            var account = Hash.Generate();
            var txnInit = new Transaction
            {
                From = Hash.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(ParamsHolder.Pack(account, (ulong)101).ToByteArray())
            };
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(address, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply();
            #endregion initialize account balance

            #region check account balance
            var txnBal = new Transaction
            {
                From = Hash.Zero,
                To = address,
                IncrementId = NewIncrementId(),
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsHolder.Pack(account).ToByteArray())
            };
            var txnBalCtxt = new TransactionContext()
            {
                Transaction = txnBal
            };
            await executiveUser.SetTransactionContext(txnBalCtxt).Apply();

            Assert.Equal((ulong)101, txnBalCtxt.Trace.RetVal.AnyToUInt64());
            #endregion
        }
    }
}