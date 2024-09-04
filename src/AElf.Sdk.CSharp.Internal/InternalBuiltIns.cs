using System;
using AElf.Sdk.CSharp.Spec;
using Bn254.Net;
using Nethereum.Util;

namespace AElf.Sdk.CSharp.Internal;

internal class InternalBuiltIns : IBuiltIns
{
    public static void Initialize()
    {
        // call this method to ensure this assembly is loaded in the runtime.  
    }

    public bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey)
    {
        try
        {
            var instance = new Rebex.Security.Cryptography.Ed25519();
            instance.FromPublicKey(publicKey);
            return instance.VerifyMessage(message, signature);
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public byte[] Keccak256(byte[] message)
    {
        return Sha3Keccack.Current.CalculateHash(message);
    }

    public (byte[] x, byte[] y) Bn254G1Mul(byte[] x1, byte[] y1, byte[] s)
    {
        var (xUInt256, yUInt256) = Bn254.Net.Bn254.Mul(UInt256.FromBigEndianBytes(x1), UInt256.FromBigEndianBytes(y1),
            UInt256.FromBigEndianBytes(s));
        return (xUInt256.ToBigEndianBytes(), yUInt256.ToBigEndianBytes());
    }

    public (byte[] x3, byte[] y3) Bn254G1Add(byte[] x1, byte[] y1, byte[] x2, byte[] y2)
    {
        var (x3UInt256, y3UInt256) = Bn254.Net.Bn254.Add(UInt256.FromBigEndianBytes(x1), UInt256.FromBigEndianBytes(y1),
            UInt256.FromBigEndianBytes(x2), UInt256.FromBigEndianBytes(y2));
        return (x3UInt256.ToBigEndianBytes(), y3UInt256.ToBigEndianBytes());
    }

    public bool Bn254Pairing((byte[], byte[], byte[], byte[], byte[], byte[])[] input)
    {
        var elements = new (UInt256, UInt256, UInt256, UInt256, UInt256, UInt256)[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var (x1, y1, x2, y2, x3, y3) = input[i];
            elements[i] = (UInt256.FromBigEndianBytes(x1), UInt256.FromBigEndianBytes(y1),
                UInt256.FromBigEndianBytes(x2), UInt256.FromBigEndianBytes(y2),
                UInt256.FromBigEndianBytes(x3), UInt256.FromBigEndianBytes(y3));
        }

        return Bn254.Net.Bn254.Pairing(elements);
    }
}