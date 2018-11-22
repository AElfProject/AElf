using System;
using Google.Protobuf;

namespace AElf.Common
{
    public static class ContractHelpers
    {
        public static Address GetSystemContractAddress(Hash chainId, UInt64 serialNumber)
        {
            return Address.FromRawBytes(Hash.FromTwoHashes(chainId, Hash.FromString(serialNumber.ToString())).ToByteArray());
        }
        
        public static Address GetGenesisBasicContractAddress(Hash chainId)
        {
            return Address.FromRawBytes(Hash.FromTwoHashes(chainId, Hash.FromString(GlobalConfig.GenesisBasicContract.ToString())).ToByteArray());
        }
        
        public static Address GetConsensusContractAddress(Hash chainId)
        {
            return Address.FromRawBytes(Hash.FromTwoHashes(chainId, Hash.FromString(GlobalConfig.ConsensusContract.ToString())).ToByteArray());
        }
        
        public static Address GetTokenContractAddress(Hash chainId)
        {
            return Address.FromRawBytes(Hash.FromTwoHashes(chainId, Hash.FromString(GlobalConfig.TokenContract.ToString())).ToByteArray());
        }
        
        public static Address GetSideChainContractAddress(Hash chainId)
        {
            return Address.FromRawBytes(Hash.FromTwoHashes(chainId, Hash.FromString(GlobalConfig.SideChainContract.ToString())).ToByteArray());
        }

        public static string GetSystemContractName(UInt64 serialNumber)
        {
            switch (serialNumber)
            {
                    case 0:
                        return "BasicContractZero";
                    case 1:
                        return "AElfDPoS";
                    case 2:
                        return "TokenContract";
                    case 3:
                        return "SideChainContract";
                    default:
                        throw new ArgumentException($"SerialNumber:{serialNumber} not exist.");
            }
        }
    }
}