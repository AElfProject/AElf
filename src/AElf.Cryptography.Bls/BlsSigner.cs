// SPDX-FileCopyrightText: 2024 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System.Buffers;
using System.Runtime.CompilerServices;
using static Nethermind.Crypto.Bls;

namespace AElf.Cryptography.Bls;

// https://www.ietf.org/archive/id/draft-irtf-cfrg-bls-signature-05.html

using G1 = P1;
using G2 = P2;
using G1Affine = P1Affine;
using G2Affine = P2Affine;
using GT = PT;

public static class BlsSigner
{
    public const int PkCompressedSz = 384 / 8;
    private static readonly byte[] CryptoSuite = AElfCryptographyBlsConstants.Dst;
    private const int InputLength = 64;

    [SkipLocalsInit]
    // buf must be of size G2.Sz
    public static Signature Sign(Span<long> buf, SecretKey sk, ReadOnlySpan<byte> message)
    {
        if (buf.Length != G2.Sz)
        {
            throw new ArgumentException($"Signature buffer {nameof(buf)} must be of size {G2.Sz}.");
        }

        G2 p = new(buf);
        Signature s = new(p);
        s.Sign(sk, message);
        return s;
    }

    public static Signature Sign(SecretKey sk, ReadOnlySpan<byte> message)
        => Sign(new long[G2.Sz], sk, message);

    [SkipLocalsInit]
    public static bool Verify(G1Affine publicKey, Signature signature, ReadOnlySpan<byte> message)
    {
        var arrayPool = ArrayPool<long>.Shared;
        var buffer = arrayPool.Rent(2 * GT.Sz);

        try
        {
            var bufSpan = buffer.AsSpan();
            GT p1 = new(bufSpan[..GT.Sz]);
            p1.MillerLoop(signature.Point, G1Affine.Generator(stackalloc long[G1Affine.Sz]));

            G2 m = new(stackalloc long[G2.Sz]);
            m.HashTo(message, CryptoSuite);
            GT p2 = new(bufSpan[GT.Sz..]);
            p2.MillerLoop(m.ToAffine(), publicKey);

            return GT.FinalVerify(p1, p2);
        }
        finally
        {
            arrayPool.Return(buffer);
        }
    }

    [SkipLocalsInit]
    public static bool Verify(G1Affine publicKey, ReadOnlySpan<byte> sigBytes, ReadOnlySpan<byte> message)
    {
        G2 p = new(stackalloc long[G2.Sz]);
        Signature s = new(p);

        if (!p.TryDecode(sigBytes, out _))
        {
            return false;
        }

        return Verify(publicKey, s, message);
    }

    public static bool VerifyAggregate(AggregatedPublicKey aggregatedPublicKey, Signature signature,
        ReadOnlySpan<byte> message)
        => Verify(aggregatedPublicKey.PublicKey, signature, message);

    public readonly ref struct Signature
    {
        public const int Sz = 96;

        public readonly ReadOnlySpan<byte> Bytes => _point.Compress();

        public readonly G2Affine Point => _point.ToAffine();

        private readonly G2 _point;

        public Signature()
        {
            _point = new();
        }

        public Signature(G2 point)
        {
            _point = point;
        }

        public void Decode(ReadOnlySpan<byte> bytes)
            => _point.Decode(bytes);

        public void Sign(SecretKey sk, ReadOnlySpan<byte> message)
        {
            _point.HashTo(message, CryptoSuite);
            _point.SignWith(sk);
        }

        public void Aggregate(Signature s)
            => _point.Aggregate(s.Point);
    }

    public readonly ref struct AggregatedPublicKey
    {
        public G1Affine PublicKey => _point.ToAffine();

        private readonly G1 _point;

        public AggregatedPublicKey()
        {
            _point = new();
        }

        public AggregatedPublicKey(Span<long> buf)
        {
            if (buf.Length != G1.Sz)
            {
                throw new ArgumentException($"Public key buffer {nameof(buf)} must be of size {G1.Sz}.");
            }

            _point = new(buf);
        }

        public void FromSk(SecretKey sk)
            => _point.FromSk(sk);

        public void Reset()
            => _point.Zero();

        public bool TryDecode(ReadOnlySpan<byte> publicKeyBytes, out ERROR err)
            => _point.TryDecode(publicKeyBytes, out err);

        public void Decode(ReadOnlySpan<byte> publicKeyBytes)
            => _point.Decode(publicKeyBytes);

        public void Aggregate(G1Affine publicKey)
            => _point.Aggregate(publicKey);

        public void Aggregate(AggregatedPublicKey aggregatedPublicKey)
            => _point.Aggregate(aggregatedPublicKey.PublicKey);

        [SkipLocalsInit]
        public bool TryAggregate(ReadOnlySpan<byte> publicKeyBytes, out ERROR err)
        {
            G1Affine pk = new(stackalloc long[G1Affine.Sz]);

            if (!pk.TryDecode(publicKeyBytes, out err))
            {
                return false;
            }

            Aggregate(pk);
            return true;
        }
    }
}