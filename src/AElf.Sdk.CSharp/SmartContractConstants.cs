using AElf.Types;

namespace AElf.Sdk.CSharp
{
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemName = Hash.FromString("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemName = Hash.FromString("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemName = Hash.FromString("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemName = Hash.FromString("AElf.ContractNames.Token");
        public static readonly Hash ParliamentAuthContractSystemName = Hash.FromString("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemName = Hash.FromString("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemName = Hash.FromString("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemName = Hash.FromString("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemName = Hash.FromString("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemName = Hash.FromString("AElf.ContractNames.Economic");
    }
}