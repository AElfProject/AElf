using System;
using AElf.Common;
using AElf.Kernel;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp.ReadOnly;
using AElf.Sdk.CSharp.State;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class Context : IContextInternal
    {
        public ITransactionContext TransactionContext { get; set; }
        public ISmartContractContext SmartContractContext { get; set; }

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
    }
}