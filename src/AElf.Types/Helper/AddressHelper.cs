using AElf.Types;

namespace AElf
{
    public static class AddressHelper
    {
        public static bool VerifyFormattedAddress(string formattedAddress)
        {
            if (string.IsNullOrEmpty(formattedAddress))
                return false;
            return Base58CheckEncoding.Verify(formattedAddress);
        }

        public static Address Base58StringToAddress(string inputStr)
        {
            return Address.FromBytes(Base58CheckEncoding.Decode(inputStr));
        }
    }
}