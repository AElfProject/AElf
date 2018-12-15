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

namespace AElf.Contracts.Consensus.Tests
{
    public class DividendsContractShim
    {
        private MockSetup _mock;
        public IExecutive Executive { get; set; }

        public TransactionContext TransactionContext { get; private set; }

        public Address Sender
        {
            get => Address.Zero;
        }
        
        public Address DividendsContractAddress { get; set; }

        public DividendsContractShim(MockSetup mock)
        {
            _mock = mock;
            Init();
        }

        private void Init()
        {
            DeployDividendsContractAsync().Wait();
            var task = _mock.GetExecutiveAsync(DividendsContractAddress);
            task.Wait();
            Executive = task.Result;
        }

        private async Task<TransactionContext> PrepareTransactionContextAsync(Transaction tx)
        {
            var chainContext = await _mock.ChainContextService.GetChainContextAsync(_mock.ChainId);
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
        }

        private async Task DeployDividendsContractAsync()
        {
            var address0 = ContractHelpers.GetGenesisBasicContractAddress(_mock.ChainId);
            var executive0 = await _mock.GetExecutiveAsync(address0);

            var tx = new Transaction
            {
                From = Sender,
                To = address0,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, _mock.GetContractCode(_mock.DividendsContractName)))
            };

            var tc = await PrepareTransactionContextAsync(tx);
            await executive0.SetTransactionContext(tc).Apply();
            await CommitChangesAsync(tc.Trace);
            DividendsContractAddress = Address.FromBytes(tc.Trace.RetVal.ToFriendlyBytes());
        }
    }
}