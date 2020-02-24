using Acs1;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationState : ContractState
    {
        public Int32State BlockTransactionLimit { get; set; }

        public SingletonState<Address> ConfigurationController { get; set; }

        public MappedState<string, MethodFees> TransactionFees { get; set; }

        /// <summary>
        /// Chain Id (of side chain) -> Resource Usage Information
        /// </summary>
        public MappedState<int, ResourceTokenAmount> RentedResourceTokenAmount { get; set; }

        public SingletonState<ResourceTokenAmount> TotalResourceTokenAmount { get; set; }

        public SingletonState<ResourceTokenAmount> RemainResourceTokenAmount { get; set; }

        public SingletonState<RequiredAcsInContracts> RequiredAcsInContracts { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}