using System;
using AElf.Common;

namespace AElf.Kernel.Types
{
    public static class ContractHelpers
    {
        public static string IndexingSideChainMethodName { get; } = "IndexSideChainBlockInfo";
        public static string IndexingParentChainMethodName { get; } = "IndexParentChainBlockInfo";
        public static Address GetSystemContractAddress(int chainId, UInt64 serialNumber)
        {
            return Address.BuildContractAddress(chainId, serialNumber);
        }
        
        public static Address GetGenesisBasicContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.GenesisBasicContract);
        }
        
        public static Address GetConsensusContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.ConsensusContract);
        }
        
        public static Address GetTokenContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.TokenContract);
        }
        
        public static Address GetCrossChainContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.CrossChainContract);
        }

        public static Address GetAuthorizationContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.AuthorizationContract);
        }

        public static Address GetResourceContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.ResourceContract);
        }

        public static Address GetDividendsContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, ContractConsts.DividendsContract);
        }
    }
}