using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Math;
using Secp256k1Net;
using ECParameters = AElf.Cryptography.ECDSA.ECParameters;

namespace AElf.Cryptography.ECVRF;

public class InvalidSerializedPublicKeyException : Exception
{
}

public class FailedToHashToCurveException : Exception
{
}

public struct Point
{
    public byte[] Inner { get; private set; }

    public static Point FromInner(byte[] inner)
    {
        return new Point() { Inner = inner };
    }

    public static Point FromSerialized(Secp256k1 secp256k1, byte[] pkSerialized)
    {
        if (pkSerialized.Length != Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH &&
            pkSerialized.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
        {
            throw new InvalidSerializedPublicKeyException();
        }

        if (pkSerialized.Length == Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH)
        {
            if (pkSerialized[0] != 0x02 && pkSerialized[0] != 0x03)
            {
                throw new InvalidSerializedPublicKeyException();
            }
        }

        if (pkSerialized.Length == Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
        {
            if (pkSerialized[0] != 0x04)
            {
                throw new InvalidSerializedPublicKeyException();
            }
        }

        var inner = new byte[Secp256k1.PUBKEY_LENGTH];
        var successful = secp256k1.PublicKeyParse(inner, pkSerialized);
        if (!successful)
        {
            throw new InvalidSerializedPublicKeyException();
        }

        return new Point() { Inner = inner };
    }

    public byte[] Serialize(Secp256k1 secp256k1, bool compressed = true)
    {
        if (compressed)
        {
            var output = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
            secp256k1.PublicKeySerialize(output, Inner, Flags.SECP256K1_EC_COMPRESSED);
            return output;
        }
        else
        {
            var output = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            secp256k1.PublicKeySerialize(output, Inner, Flags.SECP256K1_EC_UNCOMPRESSED);
            return output;
        }
    }
}

public struct VrfConfig
{
    public byte SuiteString { get; private set; }

    public VrfConfig(byte suiteString)
    {
        SuiteString = suiteString;
    }
}

public class Vrf : IVrf
{
    private const int QBitsLength = 256;
    private const int N = 16;
    private VrfConfig _config;

    public Vrf(VrfConfig config)
    {
        _config = config;
    }

    public Proof Prove(ECKeyPair keyPair, byte[] alpha)
    {
        using var secp256k1 = new Secp256k1();
        var point = Point.FromSerialized(secp256k1, keyPair.PublicKey);
        var hashPoint = HashToCurveTryAndIncrement(point, alpha);
        var gamma = new byte[Secp256k1.PUBKEY_LENGTH];
        Buffer.BlockCopy(hashPoint.Inner, 0, gamma, 0, gamma.Length);
        secp256k1.PublicKeyMultiply(gamma, keyPair.PrivateKey);
        var nonce = Rfc6979Nonce(secp256k1, keyPair, hashPoint);
        var kB = new byte[Secp256k1.PUBKEY_LENGTH];
        secp256k1.PublicKeyCreate(kB, nonce);
        var kH = new byte[Secp256k1.PUBKEY_LENGTH];
        Buffer.BlockCopy(hashPoint.Inner, 0, kH, 0, kH.Length);
        secp256k1.PublicKeyMultiply(kH, nonce);
        var c = HashPoints(secp256k1, hashPoint, Point.FromInner(gamma), Point.FromInner(kB), Point.FromInner(kH));
        var cX = c.Multiply(new BigInteger(1, keyPair.PrivateKey));
        var s = cX.Add(new BigInteger(1, nonce)).Mod(ECParameters.DomainParams.N);
        var pi = EncodeProof(Point.FromInner(gamma), c, s);
        var beta = GammaToHash(secp256k1, Point.FromInner(gamma));
        return new Proof
        {
            Pi = pi, Beta = beta
        };
    }

    public byte[] Verify(byte[] publicKey, Proof proof)
    {
        throw new System.NotImplementedException();
    }

    public Point HashToCurveTryAndIncrement(Point point, byte[] alpha)
    {
        using var secp256k1 = new Secp256k1();

        // Step 1: ctr = 0
        var ctr = 0;

        // Step 2: PK_string = point_to_string(Y)
        var pkString = point.Serialize(secp256k1, true);

        // Steps 3 ~ 6
        byte oneString = 0x01;
        for (; ctr < 256; ctr++)
        {
            using var hasher = SHA256.Create();
            using var stream = new MemoryStream();
            stream.WriteByte(_config.SuiteString);
            stream.WriteByte(oneString);
            stream.Write(pkString);
            stream.Write(alpha);
            stream.WriteByte((byte)ctr);
            stream.Seek(0, SeekOrigin.Begin);
            var hash = hasher.ComputeHash(stream);
            var pkSerialized = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
            pkSerialized[0] = 0x02;
            Buffer.BlockCopy(hash, 0, pkSerialized, 1, hash.Length);
            try
            {
                var outputPoint = Point.FromSerialized(secp256k1, pkSerialized);
                return outputPoint;
            }
            catch (InvalidSerializedPublicKeyException ex)
            {
            }
        }

        throw new FailedToHashToCurveException();
    }

    public BigInteger HashPoints(Secp256k1 secp256k1, params Point[] points)
    {
        using var hasher = SHA256.Create();
        using var stream = new MemoryStream();
        stream.WriteByte(_config.SuiteString);
        stream.WriteByte(0x02);
        foreach (var point in points)
        {
            var pkBytes = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
            secp256k1.PublicKeySerialize(pkBytes, point.Inner, Flags.SECP256K1_EC_COMPRESSED);
            stream.Write(pkBytes);
        }

        stream.Seek(0, SeekOrigin.Begin);
        var hash = hasher.ComputeHash(stream);
        var hashTruncated = hash.Take(N).ToArray();
        return new BigInteger(1, hashTruncated);
    }

    public byte[] EncodeProof(Point gamma, BigInteger c, BigInteger s)
    {
        using var secp256k1 = new Secp256k1();
        var gammaBytes = gamma.Serialize(secp256k1, true);
        var cBytes = Int2Bytes(c, N);
        var sBytes = Int2Bytes(s, (QBitsLength + 7) / 8);
        var output = new byte[gammaBytes.Length + cBytes.Length + sBytes.Length];
        Buffer.BlockCopy(gammaBytes, 0, output, 0, gammaBytes.Length);
        Buffer.BlockCopy(cBytes, 0, output, gammaBytes.Length, cBytes.Length);
        Buffer.BlockCopy(sBytes, 0, output, gammaBytes.Length + cBytes.Length, sBytes.Length);
        return output;
    }

    private static byte[] AddLeadingZeros(byte[] data, int requiredLength)
    {
        var zeroBytesLength = requiredLength - data.Length;
        if (zeroBytesLength <= 0) return data;
        var output = new byte[requiredLength];
        Buffer.BlockCopy(data, 0, output, zeroBytesLength, data.Length);
        for (int i = zeroBytesLength - 1; i >= 0; i--)
        {
            output[i] = 0x0;
        }

        return output;
    }

    public byte[] GammaToHash(Secp256k1 secp256k1, Point gamma)
    {
        var gammaBytes = gamma.Serialize(secp256k1, true);
        using var hasher = SHA256.Create();
        using var stream = new MemoryStream();
        stream.WriteByte(_config.SuiteString);
        stream.WriteByte(0x03);
        stream.Write(gammaBytes);
        stream.Seek(0, SeekOrigin.Begin);
        return hasher.ComputeHash(stream);
    }

    public byte[] Rfc6979Nonce(Secp256k1 secp256k1, ECKeyPair keyPair, Point hashPoint)
    {
        using var hasher = SHA256.Create();
        var roLen = (QBitsLength + 7) / 8;
        var hBytes = hashPoint.Serialize(secp256k1, true);

        var hash = hasher.ComputeHash(hBytes);
        var bh = Bits2Bytes(hash, ECParameters.DomainParams.N, roLen);
        
        var nonce = new byte[Secp256k1.NONCE_LENGTH];
        secp256k1.Rfc6979Nonce(nonce, bh, keyPair.PrivateKey, null, null, 0);
        return nonce;
    }

    public byte[] Int2Bytes(BigInteger v, int rolen)
    {
        var result = v.ToByteArray();
        if ( result.Length < rolen)
        {
            return AddLeadingZeros(result, rolen);
        }

        if (result.Length > rolen)
        {
            var skipLength = result.Length - rolen;
            return result.Skip(skipLength).ToArray();
        }

        return result;
    }
    
    public BigInteger Bits2Int(byte[]inputBytes, int qlen)
    {
        var output = new BigInteger(1, inputBytes);
        if (inputBytes.Length * 8 > qlen)
        {
            return output.ShiftRight(inputBytes.Length * 8 - qlen);
        }
        
        return output;
    }

    public byte[] Bits2Bytes(byte[]input, BigInteger q, int rolen)
    {
        var z1 = Bits2Int(input, q.BitLength);
        var z2 = z1.Subtract(q);
        if (z2.SignValue == -1)
        {
            return Int2Bytes(z1, rolen);
        }

        return Int2Bytes(z2, rolen);
    }
}