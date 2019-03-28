namespace AElf.Common
{
    public static class AddressHelpers
    {
        // Test only
        public static Address BuildAddress(byte[] key)
        {
            return Address.FromPublicKey(key);
        }

        // Test only 
        public static Address BuildAddress(byte[] chainId, byte[] key)
        {
            return Address.FromPublicKey(key);
        }
    }
}