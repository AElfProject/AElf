using System;
using AElf.Common;
using AElf.Kernel;
using System.Linq;
using System.Reflection;
using AElf.Kernel.Blockchain.Application;
using AElf.Sdk.CSharp.ReadOnly;
using AElf.Sdk.CSharp.State;
using AElf.Kernel.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.Sdk.CSharp
{
    public class Context : IContextInternal
    {
        private IBlockchainService _blockChain { get; set; }
        private ISmartContractContext _smartContractContext;
        public ITransactionContext TransactionContext { get; set; }

        public ISmartContractContext SmartContractContext
        {
            get => _smartContractContext;
            set
            {
                _smartContractContext = value;
                OnSmartContractContextSet();
            }
        }

        private void OnSmartContractContextSet()
        {
            _blockChain = _smartContractContext.ChainService;
        }

        public void FireEvent(Event logEvent)
        {
            TransactionContext.Trace.Logs.Add(logEvent.GetLogEvent(Self));
        }

        public Address Sender => TransactionContext.Transaction.From.ToReadOnly();
        public Address Self => SmartContractContext.ContractAddress.ToReadOnly();

        public void SendInline(Address address, string methodName, params object[] args)
        {
            TransactionContext.Trace.InlineTransactions.Add(new Transaction()
            {
                From = TransactionContext.Transaction.From,
                To = address,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(args))
            });
        }


        public Block GetPreviousBlock()
        {
            return AsyncHelper.RunSync(
                () => _blockChain.GetBlockByHashAsync(_smartContractContext.ChainId,
                    TransactionContext.PreviousBlockHash));
        }
    }
}