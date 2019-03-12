using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class DPoSContractState
    {
        public SingletonState<Miners> CurrentMiners { get; set; }
    }
}