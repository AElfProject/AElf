using AElf.Types;

namespace AElf.Sdk.CSharp
{
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemName = Hash.FromString("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemName = Hash.FromString("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemName = Hash.FromString("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemName = Hash.FromString("AElf.ContractNames.Token");
        public static readonly Hash ParliamentContractSystemName = Hash.FromString("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemName = Hash.FromString("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemName = Hash.FromString("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemName = Hash.FromString("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemName = Hash.FromString("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemName = Hash.FromString("AElf.ContractNames.Economic");
        public static readonly Hash ReferendumContractSystemName = Hash.FromString("AElf.ContractNames.Referendum");
        public static readonly Hash AssociationContractSystemName = Hash.FromString("AElf.ContractNames.Association");
        public static readonly Hash ConfigurationContractSystemName = Hash.FromString("AElf.ContractNames.Configuration");
        public static readonly Hash TokenHolderContractSystemName = Hash.FromString("AElf.ContractNames.TokenHolder");

        public static readonly int AElfStringLengthLimitInContract = 20000;
    }
}