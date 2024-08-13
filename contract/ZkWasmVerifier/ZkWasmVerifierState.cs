using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.ZkWasmVerifier
{
    // The state class is access the blockchain state
    public class ZkWasmVerifierState : ContractState 
    {
        // A state that holds string value
        public StringState Message { get; set; }
    }
}