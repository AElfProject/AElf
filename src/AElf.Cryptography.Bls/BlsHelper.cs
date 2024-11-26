using System.Text;

namespace AElf.Cryptography.Bls;

using G1 = Nethermind.Crypto.Bls.P1;
using G2 = Nethermind.Crypto.Bls.P2;
using G1Affine = Nethermind.Crypto.Bls.P1Affine;
using G2Affine = Nethermind.Crypto.Bls.P2Affine;
using GT = Nethermind.Crypto.Bls.PT;

public static class BlsHelper
{
    public static G1 GetBlsPubkey(byte[] privateKey)
    {
        var secretKey = new Nethermind.Crypto.Bls.SecretKey();
        secretKey.Keygen(privateKey);
        return new G1(secretKey);
    }

    public static G2 SignMessage(byte[] privateKey, byte[] message)
    {
        var secretKey = new Nethermind.Crypto.Bls.SecretKey();
        secretKey.Keygen(privateKey);
        var pubkey = new G1(secretKey);
        var dst = new ReadOnlySpan<byte>(AElfCryptographyBlsConstants.Dst);
        return new G2().HashTo(message, dst, pubkey.Serialize())
            .SignWith(secretKey);
    }

}