using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class DPoSContractState
    {
        public SingletonState<Miners> CurrentMiners { get; set; }
        public Int64State TermNumberFromMainChainField { get; set; }
    }
}