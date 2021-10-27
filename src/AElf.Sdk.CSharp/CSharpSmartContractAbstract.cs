using AElf.Types;
using AElf.Kernel.SmartContract;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract TransactionExecutingStateSet GetChanges();
        internal abstract void Cleanup();

        protected void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionException(message);
            }
        }

        internal abstract void InternalInitialize(ISmartContractBridgeContext bridgeContext);
        
        /// <summary>
        /// Represents the transaction execution context in a smart contract. It provides access inside the contract to
        /// properties and methods useful for implementing the smart contracts action logic.
        /// </summary>
        public CSharpSmartContractContext Context { get; set; }
    }
}