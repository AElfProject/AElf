namespace AElf.Contracts.Consensus.DPoS
{
    public partial class DPoSContractState
    {
        public DividendContractReferenceState DividendContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
    }
}