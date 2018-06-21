using System.IO;
using AElf.Kernel;
using AElf.Types.CSharp;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Contracts.DPoS.Tests
{
    public class TestDPoSContractShim
    {
        private DPoSMockSetup _mock;
        public Hash ContractAddres = Hash.Generate();
        public IExecutive Executive { get; set; }

        public byte[] Code
        {
            get
            {
                string filePath =
                    "../../../../AElf.Contracts.DPoS/bin/Debug/netstandard2.0/AElf.Contracts.DPoS.dll";
                byte[] code;
                using (var file = File.OpenRead(System.IO.Path.GetFullPath(filePath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        public TestDPoSContractShim(DPoSMockSetup mock)
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

        public BlockProducer SetBlockProducers()
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = ContractAddres,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "SetBlockProducers",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            var tc = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(tc).Apply().Wait();
            return tc.Trace.RetVal.DeserializeToPbMessage<BlockProducer>();
        }
    }
}