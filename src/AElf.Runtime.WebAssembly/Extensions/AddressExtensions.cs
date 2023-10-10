using AElf.Types;
using Nethereum.Hex.HexConvertors.Extensions;

namespace AElf.Runtime.WebAssembly.Extensions;

public static class AddressExtensions
{
    public static Address EthAddressToAElfAddress(this string hexAddress)
    {
        return hexAddress.HexToByteArray().EthAddressToAElfAddress();
    }

    public static Address EthAddressToAElfAddress(this byte[] bytes)
    {
        return Address.FromBytes(bytes.LeftPad(AElfConstants.AddressHashLength));
    }
}