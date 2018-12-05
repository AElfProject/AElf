using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AElf.SmartContract;
using AElf.Kernel;
using AElf.Types.CSharp;
using Akka.IO;
using Xunit;
using ByteString = Google.Protobuf.ByteString;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Contracts.Token.Tests
{
    public class TokenContractShim
    {
        private MockSetup _mock;
        public IExecutive Executive { get; set; }

        public TransactionContext TransactionContext { get; private set; }

        public Address Sender
        {
            get => Address.Zero;
        }
        
        public Address TokenContractAddress { get; set; }

        public TokenContractShim(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            DeployTokenContractAsync().Wait();
            var task = _mock.GetExecutiveAsync(TokenContractAddress);
            task.Wait();
            Executive = task.Result;
        }

        private async Task<TransactionContext> PrepareTransactionContextAsync(Transaction tx)
        {
            var chainContext = await _mock.ChainContextService.GetChainContextAsync(_mock.ChainId1);
            var tc = new TransactionContext()
            {
                PreviousBlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight,
                Transaction = tx,
                Trace = new TransactionTrace()
            };
            return tc;
        }

        private TransactionContext PrepareTransactionContext(Transaction tx)
        {
            var task = PrepareTransactionContextAsync(tx);
            task.Wait();
            return task.Result;
        }

        private async Task CommitChangesAsync(TransactionTrace trace)
        {
            await trace.CommitChangesAsync(_mock.StateStore);
//            await _mock.StateDictator.ApplyCachedDataAction(changes);
        }

        private async Task DeployTokenContractAsync()
        {
            
            var address0 = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId1);
            var executive0 = await _mock.GetExecutiveAsync(address0);

            var tx = new Transaction
            {
                From = Sender,
                To = address0,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.TokenCode))
            };

            var tc = await PrepareTransactionContextAsync(tx);
            await executive0.SetTransactionContext(tc).Apply();
            await CommitChangesAsync(tc.Trace);
            TokenContractAddress = Address.FromBytes(tc.Trace.RetVal.ToFriendlyBytes());
        }

        #region ABI (Public) Methods

        #region View Only Methods

        public string Symbol()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Symbol",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public string TokenName()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "TokenName",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToString();
        }

        public ulong TotalSupply()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "TotalSupply",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        public uint Decimals()
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Decimals",
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt32()??0;
        }

        public ulong BalanceOf(Hash owner)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "BalanceOf",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        public ulong Allowance(Address owner, Address spender)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Allowance",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(owner, spender))
            };

            TransactionContext = new TransactionContext()
            {
                Transaction = tx
            };
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            TransactionContext.Trace.CommitChangesAsync(_mock.StateStore).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToUInt64()??0;
        }

        #endregion View Only Methods


        #region Actions

        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(symbol, tokenName, totalSupply, decimals))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void Transfer(Address to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(to, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void TransferFrom(Address from, Address to, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "TransferFrom",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(from, to, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void Approve(Address spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "Approve",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public void UnApprove(Address spender, ulong amount)
        {
            var tx = new Transaction
            {
                From = Sender,
                To = TokenContractAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "UnApprove",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(spender, amount))
            };

            TransactionContext = PrepareTransactionContext(tx);
            Executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
        }

        public Address GetContractOwner(Address scZeroAddress)
        {
            var executive = _mock.GetExecutiveAsync(scZeroAddress).Result;
            
            var tx = new Transaction
            {
                From = Sender,
                To = scZeroAddress,
                IncrementId = _mock.NewIncrementId(),
                MethodName = "GetContractOwner",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(TokenContractAddress))
            };

            TransactionContext = PrepareTransactionContext(tx);
            executive.SetTransactionContext(TransactionContext).Apply().Wait();
            CommitChangesAsync(TransactionContext.Trace).Wait();
            return TransactionContext.Trace.RetVal?.Data.DeserializeToPbMessage<Address>();
        }
        
        #endregion Actions

        #endregion ABI (Public) Methods

    }
}