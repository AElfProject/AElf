using System;
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

        /// <summary>
        /// Creates an address from a string. This method is supposed to be used for test only.
        /// The hash bytes of the string will be used to create the address.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Address FromString(string name)
        {
            return Address.FromBytes(name.CalculateHash());
        }
    }
}