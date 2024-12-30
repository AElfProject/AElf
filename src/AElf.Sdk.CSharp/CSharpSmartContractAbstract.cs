using AElf.Kernel.SmartContract;
using AElf.Types;

namespace AElf.Sdk.CSharp;

public abstract class CSharpSmartContractAbstract : CSharpSmartContract
{
    /// <summary>
    ///     Represents the transaction execution context in a smart contract. It provides access inside the contract to
    ///     properties and methods useful for implementing the smart contracts action logic.
    /// </summary>
    public CSharpSmartContractContext Context { get; set; }

    internal abstract TransactionExecutingStateSet GetChanges();
    internal abstract void Cleanup();

    protected void Assert(bool asserted, string message = "Assertion failed!")
    {
        if (!asserted) throw new AssertionException(message);
    }

    internal abstract void InternalInitialize(ISmartContractBridgeContext bridgeContext);
}