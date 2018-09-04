using AElf.Kernel;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Contracts.SideChain.Tests
{
    public class ContractZeroShim
    {
        private MockSetup _mock;
        public Hash ContractAddres = Hash.Generate();
        public IExecutive Executive { get; set; }

        public ITransactionContext TransactionContext { get; private set; }

        public Hash Sender
        {
            get => Hash.Zero;
        }
        
        public Hash Address
        {
            get => new Hash(_mock.ChainId1.CalculateHashWith(SmartContractType.TokenContract.ToString())).ToAccount();
        }
        
        public ContractZeroShim(MockSetup mock)
        {
            _mock = mock;
            Initialize();
        }

        private void Initialize()
        {
            var task = _mock.GetExecutiveAsync(Address);
            task.Wait();
            Executive = task.Result;
        }

        public byte[] DeploySmartContract(int category, byte[] code)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(category, code))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateDictator).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBytes();
        }

        public void ChangeContractOwner(Hash contractAddress, Hash newOwner)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "ChangeContractOwner",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(contractAddress, newOwner))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateDictator).Wait();
        }
        
        public Hash GetContractOwner(Hash contractAddress)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetContractOwner",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(contractAddress))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateDictator).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Hash>();
        }
    }
}