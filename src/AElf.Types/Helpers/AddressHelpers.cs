namespace AElf
{
    // TODO: check helper or helpers?
    public static class AddressHelpers
    {
        public static bool VerifyFormattedAddress(string formattedAddress)
        {
            if (string.IsNullOrEmpty(formattedAddress))
                return false;
            return Base58CheckEncoding.Verify(formattedAddress);
        }
    }
}