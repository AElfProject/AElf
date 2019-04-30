using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class DPoSContractState
    {
        public BoolState IsStrategyConfigured { get; set; }
        public BoolState IsBlockchainAgeSettable { get; set; }
        public BoolState IsTimeSlotSkippable { get; set; }
        public BoolState IsVerbose { get; set; }
    }
}