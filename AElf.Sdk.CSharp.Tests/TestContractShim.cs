using System.IO;
using AElf.Kernel;
using AElf.SmartContract;
using ServiceStack;
using Google.Protobuf;
using AElf.Types.CSharp;

namespace AElf.Sdk.CSharp.Tests
{
    public class TestContractShim
    {
        private MockSetup _mock;
        public Hash ContractAddres = Hash.Generate();
        public IExecutive Executive { get; set; }

        public byte[] Code
        {
            get
            {
                string filePath =
                    "../../../../AElf.Sdk.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/AElf.Sdk.CSharp.Tests.TestContract.dll";
                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(filePath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        public TestContractShim(MockSetup mock)
        {
            _mock = mock;
            Initialize();
        }

        private void Initialize()
        {
            _mock.DeployContractAsync(Code, ContractAddres).Wait();
            var task = _mock.GetExecutiveAsync(ContractAddres);
            task.Wait();
            Executive = task.Result;
        }

        public uint GetTotalSupply()
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetTotalSupply",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply(true).Wait();
            return tc.Trace.RetVal.Data.DeserializeToUInt32();
        }

        public bool SetAccount(string name, Hash address)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "SetAccount",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(name, address))
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply(true).Wait();
            return tc.Trace.RetVal.Data.DeserializeToBool();
        }

        public string GetAccountName()
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetAccountName",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply(true).Wait();
            return tc.Trace.RetVal.Data.DeserializeToString();
        }
    }
}