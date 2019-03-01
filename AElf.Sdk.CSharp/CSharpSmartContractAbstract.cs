using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract void SetSmartContractContext(ISmartContractContext smartContractContext);
        internal abstract void SetTransactionContext(ITransactionContext transactionContext);
        internal abstract void SetStateProvider(IStateProvider stateProvider);
        internal abstract void SetContractAddress(Address address);
        internal abstract TransactionExecutingStateSet GetChanges();
        internal abstract void Cleanup();

        protected void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }
    }
}