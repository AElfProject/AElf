using AElf.Types;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Static class containing the hashes built from the names of the contracts.
    /// </summary>
    public static class SmartContractConstants
    {
        public static readonly Hash ElectionContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Election");
        public static readonly Hash TreasuryContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Treasury");
        public static readonly Hash ConsensusContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Consensus");
        public static readonly Hash TokenContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Token");
        public static readonly Hash ParliamentContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Parliament");
        public static readonly Hash VoteContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Vote");
        public static readonly Hash ProfitContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Profit");
        public static readonly Hash CrossChainContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenConverterContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.TokenConverter");
        public static readonly Hash EconomicContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Economic");
        public static readonly Hash ReferendumContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Referendum");
        public static readonly Hash AssociationContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Association");
        public static readonly Hash ConfigurationContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Configuration");
        public static readonly Hash TokenHolderContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.TokenHolder");

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