using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationState : ContractState
    {
        public Int32State BlockTransactionLimit { get; set; }
        
        public SingletonState<Address> Owner { get; set; }
    }
}