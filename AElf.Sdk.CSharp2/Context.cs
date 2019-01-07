using AElf.Common;
using AElf.Kernel;
using System.Linq;
using System.Reflection;
using AElf.Sdk.CSharp.ReadOnly;
using AElf.SmartContract;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public class Context : IContext
    {
        public ITransactionContext TransactionContext { get; set; }
        public ISmartContractContext SmartContractContext { get; set; }

        public void FireEvent(Event logEvent)
        {
            TransactionContext.Trace.Logs.Add(logEvent.GetLogEvent(Self));
        }

        public Address Sender => TransactionContext.Transaction.From.ToReadOnly();
        public Address Self => SmartContractContext.ContractAddress.ToReadOnly();
    }
}