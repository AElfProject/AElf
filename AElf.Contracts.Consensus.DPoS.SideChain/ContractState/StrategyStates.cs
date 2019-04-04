using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class DPoSContractState
    {
        public BoolState IsStrategyConfigured { get; set; }
        public BoolState IsVerbose { get; set; }
    }
}