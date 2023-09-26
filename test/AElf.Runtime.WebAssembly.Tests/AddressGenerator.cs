using AElf.Types;

namespace AElf.Runtime.WebAssembly.Tests;

public class AddressGenerator
{
    public Address GenerateContractAddress(Address deployingAddress, Hash codeHash, byte[] inputData, byte[] salt)
    {
        var hash = HashHelper.ComputeFrom(deployingAddress.ToByteArray().Concat(codeHash.ToByteArray())
            .Concat(inputData).Concat(salt).ToArray());
        return Address.FromBytes(hash.ToByteArray());
    }
}