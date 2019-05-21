using Acs0;
using AElf.Contracts.Dividend;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class DPoSContractState
    {
        internal DividendContractContainer.DividendContractReferenceState DividendContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
        
        public SingletonState<Hash> DividendContractSystemName { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
    }
}