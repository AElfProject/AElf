using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> : CSharpSmartContractAbstract
        where TContractState : ContractState
    {
        internal override void SetSmartContractContext(ISmartContractContext smartContractContext)
        {
            _context.SmartContractContext = smartContractContext;
        }

        internal override void SetTransactionContext(ITransactionContext transactionContext)
        {
            _context.TransactionContext = transactionContext;
            SetContractAddress(transactionContext.Transaction.To);
            State.Provider.TransactionContext = transactionContext;
        }

        internal override void SetStateProvider(IStateProvider stateProvider)
        {
            State.Provider = stateProvider;
        }

        internal override void SetContractAddress(Address address)
        {
            if (address == null)
            {
                throw new Exception($"Input {nameof(address)} is null.");
            }

            var path = new StatePath();
            path.Path.Add(ByteString.CopyFromUtf8(address.GetFormatted()));
            State.Path = path;
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            return State.GetChanges();
        }

        internal override void Cleanup()
        {
            State.Clear();
        }
    }
}