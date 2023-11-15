using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;

namespace AElf.Runtime.WebAssembly.Types;

public static class WebAssemblyAddressExtension
{
    private const string AbiType = "bytes32";

    public static ABIValue ToWebAssemblyAddress(this byte[] bytes)
    {
        return new ABIValue(AbiType, bytes);
    }

    public static ABIValue ToWebAssemblyAddress(this Address address)
    {
        return new ABIValue(AbiType, address.ToByteArray());
    }
}