using AElf.Types;

namespace Scale.Encoders;

public class AddressTypeEncoder
{
    /// <summary>
    /// aelf address, base58 encoded.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public byte[] Encode(string address)
    {
        return Address.FromBase58(address).ToByteArray();
    }

    public byte[] Encode(Address address)
    {
        return address.ToByteArray();
    }
}