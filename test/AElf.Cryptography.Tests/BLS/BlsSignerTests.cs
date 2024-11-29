using System;
using AElf.Cryptography.Bls;
using Shouldly;
using Xunit;
using static Nethermind.Crypto.Bls;

namespace AElf.Cryptography.Tests.BLS;

using G1 = P1;

public class BlsSignerTests
{
    private static readonly string SecretKey = "2cd4ba406b522459d57a0bed51a397435c0bb11dd5f3ca1152b3694bb91d7c22";
    private static readonly byte[] SecretKeyBytes = ByteArrayHelper.HexStringToByteArray(SecretKey);

    private static readonly byte[] MessageBytes =
    [
        0x3e, 0x00, 0xef, 0x2f, 0x89, 0x5f, 0x40, 0xd6, 0x7f, 0x5b, 0xb8, 0xe8, 0x1f, 0x09, 0xa5, 0xa1, 0x2c, 0x84,
        0x0e, 0xc3, 0xce, 0x9a, 0x7f, 0x3b, 0x18, 0x1b, 0xe1, 0x88, 0xef, 0x71, 0x1a, 0x1e
    ];

    private static readonly int AggregateSignerCount = 100;

    [Fact]
    public void CalculateSignatureTest()
    {
        const string expected =
            "92266e94804ae2e3b21a31d1df347e6d869bf1351da8bf31fae71992b508cd6933df1bc9f3cddfd25bb56cef95198bbf0a553e3b375840d2ccb5222038058e43e7724dbdf093aff43a61fbccba176756e6ba5e510fb2824e19648675fa28bb70";
        SecretKey secretKey = new(SecretKeyBytes, ByteOrder.LittleEndian);
        var signature = BlsSigner.Sign(secretKey, MessageBytes);
        signature.Bytes.ToArray().ToHex().ShouldBe(expected);
    }

    [Fact]
    public void VerifySignatureTest()
    {
        SecretKey secretKey = new(SecretKeyBytes, ByteOrder.LittleEndian);
        var s = BlsSigner.Sign(secretKey, MessageBytes);
        G1 pubkey = new();
        pubkey.FromSk(secretKey);
        BlsSigner.Verify(pubkey.ToAffine(), s, MessageBytes).ShouldBeTrue();
    }

    [Fact]
    public void VerifyAggregateSignature()
    {
        BlsSigner.Signature aggregatedSignature = new();
        BlsSigner.AggregatedPublicKey aggregatedPubkey = new();

        SecretKey masterSk = new(SecretKeyBytes, ByteOrder.LittleEndian);

        for (var i = 0; i < AggregateSignerCount; i++)
        {
            SecretKey secretKey = new(masterSk, (uint)i);

            // Sign and aggregate signature
            BlsSigner.Signature signature = new();
            signature.Sign(secretKey, MessageBytes);
            aggregatedSignature.Aggregate(signature);

            // Aggregate pubkey who signed
            G1 pubkey = new();
            pubkey.FromSk(secretKey);
            aggregatedPubkey.Aggregate(pubkey.ToAffine());
        }

        BlsSigner.VerifyAggregate(aggregatedPubkey, aggregatedSignature, MessageBytes).ShouldBeTrue();
    }

    [Fact]
    public void RejectsBadSignature()
    {
        SecretKey secretKey = new(SecretKeyBytes, ByteOrder.LittleEndian);
        var signature = BlsSigner.Sign(secretKey, MessageBytes);
        Span<byte> badSig = stackalloc byte[96];
        signature.Bytes.CopyTo(badSig);
        badSig[34] += 1;

        G1 publicKey = new();
        publicKey.FromSk(secretKey);
        BlsSigner.Verify(publicKey.ToAffine(), badSig, MessageBytes).ShouldBeFalse();
    }

    [Fact]
    public void RejectsMissingAggregateSignature()
    {
        BlsSigner.Signature agg = new();
        BlsSigner.Signature s = new();
        BlsSigner.AggregatedPublicKey aggregatedPublicKey = new();
        G1 pk = new();

        SecretKey masterSk = new(SecretKeyBytes, ByteOrder.LittleEndian);

        for (var i = 0; i < AggregateSignerCount; i++)
        {
            SecretKey secretKey = new(masterSk, (uint)i);
            s.Sign(secretKey, MessageBytes);
            if (i != 0)
            {
                // exclude one signature
                agg.Aggregate(s);
            }

            pk.FromSk(secretKey);
            aggregatedPublicKey.Aggregate(pk.ToAffine());
        }

        BlsSigner.VerifyAggregate(aggregatedPublicKey, agg, MessageBytes).ShouldBeFalse();
    }
}