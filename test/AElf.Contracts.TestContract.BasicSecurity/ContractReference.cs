using AElf.Contracts.TestContract.BasicFunction;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.BasicSecurity
{
        public partial class BasicSecurityContractState        {
            internal BasicFunctionContractContainer.BasicFunctionContractReferenceState BasicFunctionContract { get; set; }
            internal Acs0.ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
            public SingletonState<Hash> Basic1ContractSystemName { get; set; }
        }
}