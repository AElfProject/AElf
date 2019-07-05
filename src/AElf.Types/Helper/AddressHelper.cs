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
    }
}