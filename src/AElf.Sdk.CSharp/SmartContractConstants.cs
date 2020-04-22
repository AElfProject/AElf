using AElf.Types;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Static class containing the hashes built from the names of the contracts.
    /// </summary>
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Token");
        public static readonly Hash ParliamentContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Economic");
        public static readonly Hash ReferendumContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Referendum");
        public static readonly Hash AssociationContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Association");
        public static readonly Hash ConfigurationContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Configuration");
        public static readonly Hash TokenHolderContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.TokenHolder");

        public static readonly int AElfStringLengthLimitInContract = 20000;
    }
}