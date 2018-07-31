using AElf.SmartContract;
using AElf.Kernel;
using Google.Protobuf;
using AElf.Types.CSharp;

namespace AElf.Contracts.Token.Tests
{
    public class TokenContractShim
    {
        private MockSetup _mock;
        public Hash ContractAddres = Hash.Generate();
        public IExecutive Executive { get; set; }

        public TransactionContext TransactionContext { get; private set; }

        public Hash Sender
        {
            get => Hash.Zero;
        }
        
        public Hash Address { get; set; }
        
        public TokenContractShim(MockSetup mock, Hash address)
        {
            _mock = mock;
            Address = address; 
            Init();
        }

        private void Init()
        {
            var task = _mock.GetExecutiveAsync(Address);
            task.Wait();
            Executive = task.Result;
        }

        #region ABI (Public) Methods

        #region View Only Methods

        public string Symbol()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Symbol",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public string TokenName()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "TokenName",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong TotalSupply()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "TotalSupply",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        public uint Decimals()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Decimals",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt32()??0;
        }

        public ulong BalanceOf(Hash owner)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "BalanceOf",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        public ulong Allowance(Hash owner, Hash spender)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Allowance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner, spender))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        #endregion View Only Methods


        #region Actions

        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(symbol, tokenName, totalSupply, decimals))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
        }

        public void Transfer(Hash to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(to, amount))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
        }

        public void TransferFrom(Hash from, Hash to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "TransferFrom",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, amount))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
        }

        public void Approve(Hash spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Approve",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
        }

        public void UnApprove(Hash spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = Address,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "UnApprove",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply(true).Wait();
        }

        #endregion Actions

        #endregion ABI (Public) Methods

    }
}