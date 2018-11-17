using Google.Protobuf;

namespace AElf.Common
{
    public static class AddressHelpers
    {
        public static Address GetSystemContractAddress(Hash chainId, string contractName)
        {
            return Address.FromRawBytes(Hash.FromTwoHashes(chainId, Hash.FromString(contractName)).ToByteArray());
        }
    }
}