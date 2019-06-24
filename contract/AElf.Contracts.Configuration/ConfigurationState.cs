using AElf.Sdk.CSharp.State;

namespace Configuration
{
    public class ConfigurationState : ContractState
    {
        public Int32State BlockTransactionLimit { get; set; }
    }
}