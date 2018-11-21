using Base58Check;
using Google.Protobuf;

namespace AElf.Common
{
    public static class AddressHelpers
    {
        public static Address GetSystemContractAddress(Hash chainId, string contractName)
        {
            return Address.BuildContractAddress(chainId.DumpByteArray(), contractName);
        }
        
        public static Address BuildAddress(byte[] key, string chainPrefix)
        {
            return Address.FromPublicKey(key, new byte[] {});
        }
    }
}