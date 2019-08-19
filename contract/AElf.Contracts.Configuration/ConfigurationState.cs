using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Configuration
{
    public class ConfigurationState : ContractState
    {
        public Int32State BlockTransactionLimit { get; set; }
    }
}