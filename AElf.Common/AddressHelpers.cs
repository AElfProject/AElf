using Base58Check;
using Google.Protobuf;

namespace AElf.Common
{
    public static class AddressHelpers
    {
        // Test only
        public static Address BuildAddress(byte[] key)
        {
            return Address.FromPublicKey(new byte[] {0x01, 0x02, 0x03}, key);
        }
        
        // Test only 
        public static Address BuildAddress(byte[] chainId, byte[] key)
        {
            return Address.FromPublicKey(chainId, key);
        }
    }
}