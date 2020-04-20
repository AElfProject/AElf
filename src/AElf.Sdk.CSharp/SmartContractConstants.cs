using AElf.Types;

namespace AElf.Sdk.CSharp
{
    /// <summary>
    /// Static class containing the hashes built from the names of the contracts.
    /// </summary>
    public static class SmartContractConstants
    {
        public static readonly string ElectionContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Election").Value.ToBase64();
        public static readonly string TreasuryContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Treasury").Value.ToBase64();
        public static readonly string ConsensusContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Consensus").Value.ToBase64();
        public static readonly string TokenContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Token").Value.ToBase64();
        public static readonly string ParliamentContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Parliament").Value.ToBase64();
        public static readonly string VoteContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Vote").Value.ToBase64();
        public static readonly string ProfitContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Profit").Value.ToBase64();
        public static readonly string CrossChainContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.CrossChain").Value.ToBase64();
        public static readonly string TokenConverterContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.TokenConverter").Value.ToBase64();
        public static readonly string EconomicContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Economic").Value.ToBase64();
        public static readonly string ReferendumContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Referendum").Value.ToBase64();
        public static readonly string AssociationContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Association").Value.ToBase64();
        public static readonly string ConfigurationContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.Configuration").Value.ToBase64();
        public static readonly string TokenHolderContractSystemName = HashHelper.ComputeFromString("AElf.ContractNames.TokenHolder").Value.ToBase64();
        
        public static readonly Hash ParliamentContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Parliament");
        public static readonly Hash CrossChainContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.CrossChain");
        public static readonly Hash TokenContractSystemHashName = HashHelper.ComputeFromString("AElf.ContractNames.Token");

        public static readonly int AElfStringLengthLimitInContract = 20000;
    }
}