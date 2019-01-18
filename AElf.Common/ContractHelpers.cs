using System;
using Google.Protobuf;

namespace AElf.Common
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
            return Address.BuildContractAddress(chainId, GlobalConfig.GenesisBasicContract);
        }
        
        public static Address GetConsensusContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, GlobalConfig.ConsensusContract);
        }
        
        public static Address GetTokenContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, GlobalConfig.TokenContract);
        }
        
        public static Address GetCrossChainContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, GlobalConfig.CrossChainContract);
        }

        public static Address GetAuthorizationContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, GlobalConfig.AuthorizationContract);
        }

        public static Address GetResourceContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, GlobalConfig.ResourceContract);
        }

        public static Address GetDividendsContractAddress(int chainId)
        {
            return Address.BuildContractAddress(chainId, GlobalConfig.DividendsContract);
        }
    }
}