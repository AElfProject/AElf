using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using Google.Protobuf;
using AElf.Types.CSharp;
using AElf.Common;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.CSharp2.Tests
{
    /*
    public class TestContractShim : ITransientDependency
    {
        private readonly MockSetup _mock;

        private Address ContractAddress
        {
            get
            {
                if (!_second)
                {
                    return _mock.ContractAddress1;
                }

                return _mock.ContractAddress2;
            }
        }

        private IExecutive Executive { get; set; }
        private Dictionary<StatePath, StateCache> GetEmptyCache() => new Dictionary<StatePath, StateCache>();

        private int ChainId
        {
            get
            {
                if (!_second)
                {
                    return _mock.ChainId1;
                }

                return _mock.ChainId2;
            }
        }

        private readonly bool _second = false;

        public TestContractShim(MockSetup mock, bool second = false)
        {
            _mock = mock;
            _second = second;
            SetExecutive();
        }

        private void SetExecutive()
        {
            Task<IExecutive> task = null;
            if (!_second)
            {
                task = _mock.SmartContractService.GetExecutiveAsync(_mock.ContractAddress1, _mock.ChainId1);
                task.Wait();
                Executive = task.Result;
                Executive.SetSmartContractContext(new SmartContractContext()
                {
                    ChainId = _mock.ChainId1,
                    ContractAddress = _mock.ContractAddress1,
                    DataProvider = _mock.DataProvider1,
                    SmartContractService = _mock.SmartContractService,
                    ChainService =  _mock.ChainService
                });
            }
            else
            {
                task = _mock.SmartContractService.GetExecutiveAsync(_mock.ContractAddress2, _mock.ChainId2);
                task.Wait();
                Executive = task.Result;
                Executive.SetSmartContractContext(new SmartContractContext()
                {
                    ChainId = _mock.ChainId2,
                    ContractAddress = _mock.ContractAddress2,
                    DataProvider = _mock.DataProvider2,
                    SmartContractService = _mock.SmartContractService,
                    ChainService =  _mock.ChainService
                });
            }
        }
        public ulong GetMethodFee(string methodName)
        {
            var tx = new Transaction
            {
                From = Address.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = nameof(GetMethodFee),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(methodName))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetDataCache(GetEmptyCache());
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.Data.DeserializeToUInt64();
        }
        public void SetMethodFee(string methodName, ulong fee)
        {
            var tx = new Transaction
            {
                From = Address.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = nameof(SetMethodFee),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(methodName, fee))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetDataCache(GetEmptyCache());
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();   
        }

        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            var tx = new Transaction
            {
                From = Address.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = nameof(Initialize),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(symbol, tokenName, totalSupply, decimals))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetDataCache(GetEmptyCache());
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
        }

        public bool Transfer(Address from, Address to, ulong qty)
        {
            var tx = new Transaction
            {
                From = from,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = nameof(Transfer),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(to, qty))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetDataCache(GetEmptyCache());
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            return tc.Trace.RetVal.Data.DeserializeToBool();
        }
        public bool TransferAndGetTrace(Address from, Address to, ulong qty, out TransactionTrace trace)
        {
            var tx = new Transaction
            {
                From = from,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = nameof(Transfer),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(to, qty))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetDataCache(GetEmptyCache());
            Executive.SetTransactionContext(tc).Apply().Wait();
            tc.Trace.SmartCommitChangesAsync(_mock.StateManager).Wait();
            trace = tc.Trace;
            return tc.Trace.RetVal.Data.DeserializeToBool();
        }
        public ulong BalanceOf(Address account)
        {
            var tx = new Transaction
            {
                From = Address.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = nameof(BalanceOf),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetDataCache(GetEmptyCache());
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.Data.DeserializeToUInt64();
        }
    }
    */
}