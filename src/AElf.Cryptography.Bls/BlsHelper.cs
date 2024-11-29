using System.Text;

namespace AElf.Cryptography.Bls;
using static Nethermind.Crypto.Bls;

using G1 = Nethermind.Crypto.Bls.P1;
using G2 = Nethermind.Crypto.Bls.P2;
using G1Affine = Nethermind.Crypto.Bls.P1Affine;
using G2Affine = Nethermind.Crypto.Bls.P2Affine;
using GT = Nethermind.Crypto.Bls.PT;

public static class BlsHelper
{
    public static byte[] GetBlsPubkey(byte[] privateKey)
    {
        SecretKey secretKey = new(privateKey, ByteOrder.LittleEndian);
        G1 pubkey = new();
        pubkey.FromSk(secretKey);
        return pubkey.Serialize();
    }

    public static byte[] SignWithSecretKey(byte[] privateKey, byte[] message)
    {
        SecretKey secretKey = new(privateKey, ByteOrder.LittleEndian);
        var signature = BlsSigner.Sign(secretKey, message);
        return signature.Bytes.ToArray();
    }

    public static bool VerifySignature(byte[] signature, byte[] data, byte[] pubkey)
    {
        G1Affine publicKey = new();
        publicKey.Decode(pubkey);
        BlsSigner.Signature s = new();
        s.Decode(signature);
        return BlsSigner.Verify(publicKey, s, data);
    }

    public static byte[] AggregateSignatures(byte[][] signatures, byte[] aggregatedSignature)
    {
        BlsSigner.Signature agg = new();
        if (aggregatedSignature.Any())
        {
            agg.Decode(aggregatedSignature);
        }

        foreach (var signature in signatures)
        {
            BlsSigner.Signature s = new();
            s.Decode(signature);
            agg.Aggregate(s);
        }

        return agg.Bytes.ToArray();
    }

    public static byte[] AggregateSignatures(byte[][] signatures)
    {
        return AggregateSignatures(signatures, []);
    }
    
    public static byte[] AggregatePubkeys(byte[][] pubkeys)
    {
        BlsSigner.AggregatedPublicKey aggregatedPublicKey = new();
        foreach (var pubkey in pubkeys)
        {
            G1 pk = new();
            pk.Decode(pubkey);
            aggregatedPublicKey.Aggregate(pk.ToAffine());
        }

        return aggregatedPublicKey.PublicKey.Serialize();
    }
}