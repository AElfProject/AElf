using AElf.Types;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Static class containing the hashes built from the names of the contracts.
    /// </summary>
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Token");
        public static readonly Hash ParliamentContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Economic");
        public static readonly Hash ReferendumContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Referendum");
        public static readonly Hash AssociationContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Association");
        public static readonly Hash ConfigurationContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.Configuration");
        public static readonly Hash TokenHolderContractSystemName = HashHelper.ComputeFrom("AElf.ContractNames.TokenHolder");

        public static readonly int AElfStringLengthLimitInContract = 20000;
    }
}