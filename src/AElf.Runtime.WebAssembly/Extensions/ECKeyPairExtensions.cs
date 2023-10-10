using AElf.Cryptography.ECDSA;
using Nethereum.Signer;

namespace AElf.Runtime.WebAssembly.Extensions;

public static class ECKeyPairExtensions
{
    public static EthECKey ToEthECKey(this ECKeyPair ecKeyPair)
    {
        return new EthECKey(ecKeyPair.PrivateKey.ToHex());
    }
}