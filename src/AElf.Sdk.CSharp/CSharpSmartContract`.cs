using AElf.Sdk.CSharp.State;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// This class represents a base class for contracts written in the C# language. The generated code from the
    /// protobuf definitions will inherit from this class.
    /// </summary>
    /// <typeparam name="TContractState"></typeparam>
    public partial class CSharpSmartContract<TContractState> where TContractState : ContractState, new()
    {
        /// <summary>
        /// Represents the transaction execution context in a smart contract. It provides access inside the contract to
        /// properties and methods useful for implementing the smart contracts action logic.
        /// </summary>
        public CSharpSmartContractContext Context { get; private set; }

        /// <summary>
        /// Provides access to the State class instance. TContractState is the type of the state class defined by the
        /// contract author.
        /// </summary>
        public TContractState State { get; internal set; }

    }
}