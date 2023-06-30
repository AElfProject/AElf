using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.Cryptography.Core;
using AElf.Cryptography.ECDSA;
using Org.BouncyCastle.Math;
using Secp256k1Net;

namespace AElf.Cryptography.ECVRF;

public class Vrf<TCurve, THasherFactory> : IVrf where TCurve : IECCurve, new()
    where THasherFactory : IHasherFactory, new()
{
    private int BitSize => _config.EcParameters.Curve.FieldSize;
    private int QBitsLength => _config.EcParameters.N.BitLength;
    private int N => ((BitSize + 1) / 2 + 7) / 8;

    private readonly VrfConfig _config;
    private readonly IHasherFactory _hasherFactory;

    public Vrf(VrfConfig config)
    {
        _config = config;
        _hasherFactory = new THasherFactory();
    }

    public byte[] Prove(ECKeyPair keyPair, byte[] alpha)
    {
        using var curve = new TCurve();
        var point = curve.DeserializePoint(keyPair.PublicKey);
        var hashPoint = HashToCurveTryAndIncrement(point, alpha);
        var gamma = curve.MultiplyScalar(hashPoint, curve.DeserializeScalar(keyPair.PrivateKey));

        var nonce = Rfc6979Nonce(keyPair, hashPoint);
        var kB = curve.GetPoint(nonce);
        var kH = curve.MultiplyScalar(hashPoint, nonce);
        var c = HashPoints(hashPoint, gamma, kB, kH);
        var cX = c.Multiply(new BigInteger(1, keyPair.PrivateKey));
        var s = cX.Add(new BigInteger(1, nonce.Representation)).Mod(_config.EcParameters.N);
        return EncodeProof(gamma, c, s);
    }

    public byte[] Verify(byte[] publicKey, byte[] alpha, byte[] pi)
    {
        using var curve = new TCurve();
        var proofInput = DecodeProof(pi);

        var pkPoint = curve.DeserializePoint(publicKey);
        var hashPoint = HashToCurveTryAndIncrement(pkPoint, alpha);
        var s = curve.DeserializeScalar(proofInput.S.ToByteArray());
        var c = curve.DeserializeScalar(proofInput.C.ToByteArray());

        var sB = curve.GetPoint(s);
        var cY = curve.MultiplyScalar(pkPoint, c);
        var u = curve.Sub(sB, cY);

        var sH = curve.MultiplyScalar(hashPoint, s);
        var cGamma = curve.MultiplyScalar(proofInput.Gamma, c);
        var v = curve.Sub(sH, cGamma);

        var derivedC = HashPoints(hashPoint, proofInput.Gamma, u, v);
        if (!derivedC.Equals(proofInput.C))
        {
            throw new InvalidProofException();
        }

        return GammaToHash(proofInput.Gamma);
    }

    public IECPoint HashToCurveTryAndIncrement(IECPoint point, byte[] alpha)
    {
        using var curve = new TCurve();

        // Step 1: ctr = 0
        var ctr = 0;

        // Step 2: PK_string = point_to_string(Y)
        var pkString = curve.SerializePoint(point, true);

        // Steps 3 ~ 6
        byte oneString = 0x01;
        for (; ctr < 256; ctr++)
        {
            using var hasher = _hasherFactory.Create();
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
                var outputPoint = curve.DeserializePoint(pkSerialized);
                if (_config.EcParameters.Curve.Cofactor.CompareTo(BigInteger.One) > 0)
                {
                    return curve.MultiplyScalar(outputPoint,
                        curve.DeserializeScalar(_config.EcParameters.Curve.Cofactor.ToByteArray()));
                }

                return outputPoint;
            }
            catch (InvalidSerializedPublicKeyException ex)
            {
                // Ignore this exception and try the next ctr
            }
        }

        throw new FailedToHashToCurveException();
    }

    private BigInteger HashPoints(params IECPoint[] points)
    {
        using var curve = new TCurve();
        using var hasher = _hasherFactory.Create();
        using var stream = new MemoryStream();
        stream.WriteByte(_config.SuiteString);
        stream.WriteByte(0x02);
        foreach (var point in points)
        {
            stream.Write(curve.SerializePoint(point, true));
        }

        stream.Seek(0, SeekOrigin.Begin);
        var hash = hasher.ComputeHash(stream);
        var hashTruncated = hash.Take(N).ToArray();
        return new BigInteger(1, hashTruncated);
    }

    private byte[] EncodeProof(IECPoint gamma, BigInteger c, BigInteger s)
    {
        using var curve = new TCurve();
        var gammaBytes = curve.SerializePoint(gamma, true);
        var cBytes = Helpers.Int2Bytes(c, N);
        var sBytes = Helpers.Int2Bytes(s, (QBitsLength + 7) / 8);
        var output = new byte[gammaBytes.Length + cBytes.Length + sBytes.Length];
        Buffer.BlockCopy(gammaBytes, 0, output, 0, gammaBytes.Length);
        Buffer.BlockCopy(cBytes, 0, output, gammaBytes.Length, cBytes.Length);
        Buffer.BlockCopy(sBytes, 0, output, gammaBytes.Length + cBytes.Length, sBytes.Length);
        return output;
    }

    private ProofInput DecodeProof(byte[] pi)
    {
        using var curve = new TCurve();
        var ptLength = (BitSize + 7) / 8 + 1;
        var cLength = N;
        var sLength = (QBitsLength + 7) / 8;
        if (pi.Length != ptLength + cLength + sLength)
        {
            throw new InvalidProofLengthException();
        }

        var gammaPoint = curve.DeserializePoint(pi.Take(ptLength).ToArray());
        var c = new BigInteger(1, pi.Skip(ptLength).Take(cLength).ToArray());
        var s = new BigInteger(1, pi.TakeLast(sLength).ToArray());
        return new ProofInput()
        {
            Gamma = gammaPoint,
            C = c,
            S = s
        };
    }

    private byte[] GammaToHash(IECPoint gamma)
    {
        using var curve = new TCurve();

        var gammaCof = gamma;
        if (_config.EcParameters.Curve.Cofactor.CompareTo(BigInteger.One) > 0)
        {
            gammaCof = curve.MultiplyScalar(gamma,
                curve.DeserializeScalar(_config.EcParameters.Curve.Cofactor.ToByteArray()));
        }

        var gammaCofBytes = curve.SerializePoint(gammaCof, true);
        using var hasher = _hasherFactory.Create();
        using var stream = new MemoryStream();
        stream.WriteByte(_config.SuiteString);
        stream.WriteByte(0x03);
        stream.Write(gammaCofBytes);
        stream.Seek(0, SeekOrigin.Begin);
        return hasher.ComputeHash(stream);
    }

    private IECScalar Rfc6979Nonce(ECKeyPair keyPair, IECPoint hashPoint)
    {
        using var curve = new TCurve();
        using var hasher = _hasherFactory.Create();
        var roLen = (QBitsLength + 7) / 8;
        var hBytes = curve.SerializePoint(hashPoint, true);

        var hash = hasher.ComputeHash(hBytes);
        var bh = Helpers.Bits2Bytes(hash, _config.EcParameters.N, roLen);

        var nonce = curve.GetNonce(curve.DeserializeScalar(keyPair.PrivateKey), bh);
        return curve.DeserializeScalar(nonce);
    }
}