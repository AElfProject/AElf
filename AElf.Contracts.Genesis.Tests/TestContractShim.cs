using System;
using System.IO;
using System.Linq;
using AElf.Kernel;
using ServiceStack;
using Google.Protobuf;
using AElf.SmartContract;
using AElf.Types.CSharp;
using AElf.Common;

namespace AElf.Contracts.Genesis.Tests
{
    public class TestContractShim
    {
        private MockSetup _mock;
        public Hash ContractAddres = Hash.Generate();
        public IExecutive Executive { get; set; }

        public TransactionContext TransactionContext { get; private set; }

        public Address Sender
        {
            get => Address.Zero;
        }

        private Address Address => Address.FromBytes(_mock.ChainId1.CalculateHashWith(Kernel.Globals.GenesisBasicContract));
        
        public TestContractShim(MockSetup mock)
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

            TransactionContext = new TransactionContext
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateDictator).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToBytes();
        }

        public void ChangeContractOwner(Address contractAddress, Address newOwner)
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
        
        public Address GetContractOwner(Address contractAddress)
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
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Address>();
        }
    }
}