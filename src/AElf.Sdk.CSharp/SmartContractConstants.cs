using AElf.Types;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Static class containing the hashes built from the names of the contracts.
    /// </summary>
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Token");
        public static readonly Hash ParliamentContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Economic");
        public static readonly Hash ReferendumContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Referendum");
        public static readonly Hash AssociationContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Association");
        public static readonly Hash ConfigurationContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.Configuration");
        public static readonly Hash TokenHolderContractSystemHashName = HashHelper.ComputeFrom("AElf.ContractNames.TokenHolder");

        public static readonly string ElectionContractSystemName = GetStringName(ElectionContractSystemHashName);
        public static readonly string TreasuryContractSystemName = GetStringName(TreasuryContractSystemHashName);
        public static readonly string ConsensusContractSystemName = GetStringName(ConsensusContractSystemHashName);
        public static readonly string TokenContractSystemName = GetStringName(TokenContractSystemHashName);
        public static readonly string ParliamentContractSystemName = GetStringName(ParliamentContractSystemHashName);
        public static readonly string VoteContractSystemName = GetStringName(VoteContractSystemHashName);
        public static readonly string ProfitContractSystemName = GetStringName(ProfitContractSystemHashName);
        public static readonly string CrossChainContractSystemName = GetStringName(CrossChainContractSystemHashName);
        public static readonly string TokenConverterContractSystemName = GetStringName(TokenConverterContractSystemHashName);
        public static readonly string EconomicContractSystemName = GetStringName(EconomicContractSystemHashName);
        public static readonly string ReferendumContractSystemName = GetStringName(ReferendumContractSystemHashName);
        public static readonly string AssociationContractSystemName = GetStringName(AssociationContractSystemHashName);
        public static readonly string ConfigurationContractSystemName = GetStringName(ConfigurationContractSystemHashName);
        public static readonly string TokenHolderContractSystemName = GetStringName(TokenHolderContractSystemHashName);
        
        

        public static readonly int AElfStringLengthLimitInContract = 20000;

        private static string GetStringName(Hash hash)
        {
            return hash.Value.ToBase64();
        }
    }
}