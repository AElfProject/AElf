using AElf.Contracts.Genesis;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.BasicSecurity
{
        public partial class BasicSecurityContractState        {
            internal BasicFunctionContractContainer.BasicFunctionContractReferenceState BasicFunctionContract { get; set; }
            internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
            public SingletonState<Hash> Basic1ContractSystemName { get; set; }
        }
}