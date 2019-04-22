using AElf.Contracts.Genesis;
using AElf.Contracts.TestContract.Basic1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.Basic2
{
        public partial class Basic2ContractState        {
            internal Basic1ContractContainer.Basic1ContractReferenceState Basic1Contract { get; set; }
            internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
            public SingletonState<Hash> Basic1ContractSystemName { get; set; }
        }
}