using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Types.CSharp;
using Xunit;

namespace AElf.Runtime.CSharp.Tests
{
    public class TestContractShim
    {
        private MockSetup _mock;
        private Hash ContractAddress
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
        private bool _second = false;
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
                    DataProvider = _mock.DataProvider1.GetDataProvider(),
                    SmartContractService = _mock.SmartContractService
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
                    DataProvider = _mock.DataProvider2.GetDataProvider(),
                    SmartContractService = _mock.SmartContractService
                });
            }
        }

        public bool InitializeAsync(Hash account, ulong qty)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account, qty))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();

            return true;
        }

        public void InvokeAsync()
        {
        }

        public bool Transfer(Hash from, Hash to, ulong qty)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, qty))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToBool();
        }

        public ulong GetBalance(Hash account)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(account))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToUInt64();
        }

        public string GetTransactionStartTime(Hash transactionHash)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetTransactionStartTime",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(transactionHash))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToString();
        }

        public string GetTransactionEndTime(Hash transactionHash)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetTransactionEndTime",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(transactionHash))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToString();
        }
    }
}
