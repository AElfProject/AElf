using AElf.Types;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Static class containing the hashes built from the names of the contracts.
    /// </summary>
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Token");
        public static readonly Hash ParliamentContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemName = Hash.ComputeFrom("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemName = Hash.ComputeFrom("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Economic");
        public static readonly Hash ReferendumContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Referendum");
        public static readonly Hash AssociationContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Association");
        public static readonly Hash ConfigurationContractSystemName = Hash.ComputeFrom("AElf.ContractNames.Configuration");
        public static readonly Hash TokenHolderContractSystemName = Hash.ComputeFrom("AElf.ContractNames.TokenHolder");

        public static readonly int AElfStringLengthLimitInContract = 20000;
    }
}