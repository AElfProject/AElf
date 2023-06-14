using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Math;
using Secp256k1Net;

namespace AElf.Cryptography.ECVRF;

public class Vrf : IVrf
{
    private int BitSize => _config.EcParameters.Curve.FieldSize;
    private int QBitsLength => _config.EcParameters.N.BitLength;
    private int N => ((BitSize + 1) / 2 + 7) / 8;

    private readonly VrfConfig _config;

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
        var s = cX.Add(new BigInteger(1, nonce)).Mod(_config.EcParameters.N);
        var pi = EncodeProof(Point.FromInner(gamma), c, s);
        var beta = GammaToHash(secp256k1, Point.FromInner(gamma));
        return new Proof
        {
            Pi = pi, Beta = beta
        };
    }

    public byte[] Verify(byte[] publicKey, byte[] alpha, byte[] pi)
    {
        using var secp256k1 = new Secp256k1();
        var proofInput = DecodeProof(secp256k1, pi);

        var pkPoint = Point.FromSerialized(secp256k1, publicKey);
        var hashPoint = HashToCurveTryAndIncrement(pkPoint, alpha);
        var sBytes = Helpers.AddLeadingZeros(proofInput.S.ToByteArray(), Secp256k1.PRIVKEY_LENGTH)
            .TakeLast(Secp256k1.PRIVKEY_LENGTH).ToArray();

        var sB = new byte[Secp256k1.PUBKEY_LENGTH];
        if (!secp256k1.PublicKeyCreate(sB, sBytes))
        {
            throw new InvalidScalarException();
        }

        var cYNeg = new byte[Secp256k1.PUBKEY_LENGTH];
        Buffer.BlockCopy(pkPoint.Inner, 0, cYNeg, 0, cYNeg.Length);

        if (!secp256k1.PublicKeyMultiply(cYNeg,
                Helpers.AddLeadingZeros(proofInput.C.ToByteArray(), Secp256k1.PRIVKEY_LENGTH)))
        {
            throw new FailedToMultiplyScalarException();
        }

        if (!secp256k1.PublicKeyNegate(cYNeg))
        {
            throw new FailedToNegatePublicKeyException();
        }

        var u = new byte[Secp256k1.PUBKEY_LENGTH];

        if (!secp256k1.PublicKeysCombine(u, sB, cYNeg))
        {
            throw new FailedToCombinePublicKeysException();
        }

        var sH = new byte[Secp256k1.PUBKEY_LENGTH];
        Buffer.BlockCopy(hashPoint.Inner, 0, sH, 0, sH.Length);

        if (!secp256k1.PublicKeyMultiply(sH, sBytes))
        {
            throw new FailedToMultiplyScalarException();
        }

        var cGammaNeg = new byte[Secp256k1.PUBKEY_LENGTH];
        Buffer.BlockCopy(proofInput.Gamma.Inner, 0, cGammaNeg, 0, cGammaNeg.Length);
        if (!secp256k1.PublicKeyMultiply(cGammaNeg,
                Helpers.AddLeadingZeros(proofInput.C.ToByteArray(), Secp256k1.PRIVKEY_LENGTH)))
        {
            throw new FailedToMultiplyScalarException();
        }

        if (!secp256k1.PublicKeyNegate(cGammaNeg))
        {
            throw new FailedToNegatePublicKeyException();
        }

        var v = new byte[Secp256k1.PUBKEY_LENGTH];

        if (!secp256k1.PublicKeysCombine(v, sH, cGammaNeg))
        {
            throw new FailedToCombinePublicKeysException();
        }

        var derivedC = HashPoints(secp256k1, hashPoint, proofInput.Gamma, Point.FromInner(u), Point.FromInner(v));
        if (!derivedC.Equals(proofInput.C))
        {
            throw new InvalidProofException();
        }

        return GammaToHash(secp256k1, proofInput.Gamma);
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

    private BigInteger HashPoints(Secp256k1 secp256k1, params Point[] points)
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

    private byte[] EncodeProof(Point gamma, BigInteger c, BigInteger s)
    {
        using var secp256k1 = new Secp256k1();
        var gammaBytes = gamma.Serialize(secp256k1, true);
        var cBytes = Helpers.Int2Bytes(c, N);
        var sBytes = Helpers.Int2Bytes(s, (QBitsLength + 7) / 8);
        var output = new byte[gammaBytes.Length + cBytes.Length + sBytes.Length];
        Buffer.BlockCopy(gammaBytes, 0, output, 0, gammaBytes.Length);
        Buffer.BlockCopy(cBytes, 0, output, gammaBytes.Length, cBytes.Length);
        Buffer.BlockCopy(sBytes, 0, output, gammaBytes.Length + cBytes.Length, sBytes.Length);
        return output;
    }

    private ProofInput DecodeProof(Secp256k1 secp256k1, byte[] pi)
    {
        var ptLength = (BitSize + 7) / 8 + 1;
        var cLength = N;
        var sLength = (QBitsLength + 7) / 8;
        if (pi.Length != ptLength + cLength + sLength)
        {
            throw new InvalidProofLengthException();
        }

        var gammaPoint = Point.FromSerialized(secp256k1, pi.Take(ptLength).ToArray());
        var c = new BigInteger(1, pi.Skip(ptLength).Take(cLength).ToArray());
        var s = new BigInteger(1, pi.TakeLast(sLength).ToArray());
        return new ProofInput()
        {
            Gamma = gammaPoint,
            C = c,
            S = s
        };
    }

    private byte[] GammaToHash(Secp256k1 secp256k1, Point gamma)
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

    private byte[] Rfc6979Nonce(Secp256k1 secp256k1, ECKeyPair keyPair, Point hashPoint)
    {
        using var hasher = SHA256.Create();
        var roLen = (QBitsLength + 7) / 8;
        var hBytes = hashPoint.Serialize(secp256k1, true);

        var hash = hasher.ComputeHash(hBytes);
        var bh = Helpers.Bits2Bytes(hash, _config.EcParameters.N, roLen);

        var nonce = new byte[Secp256k1.NONCE_LENGTH];
        secp256k1.Rfc6979Nonce(nonce, bh, keyPair.PrivateKey, null, null, 0);
        return nonce;
    }
}