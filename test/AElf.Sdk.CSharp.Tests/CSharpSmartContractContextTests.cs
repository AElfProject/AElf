using System;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Bn254.Net;
using Google.Protobuf;
using Nethereum.Util;
using Shouldly;
using Xunit;
using CustomContract = AElf.Runtime.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests;

public class CSharpSmartContractContextTests : SdkCSharpTestBase
{
    // [Fact]
    // public void Verify_Ed25519Verify()
    // {
    //     var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
    //     var origin = SampleAddress.AddressList[0];
    //     bridgeContext.TransactionContext = new TransactionContext
    //     {
    //         Origin = origin,
    //         Transaction = new Transaction
    //         {
    //             From = SampleAddress.AddressList[1],
    //             To = SampleAddress.AddressList[2]
    //         }
    //     };
    //     var contractContext = new CSharpSmartContractContext(bridgeContext);
    //     var publicKey = "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a";
    //     var message = "";
    //     var signature = "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b";
    //     var ed25519VerifyResult = contractContext.Ed25519Verify(
    //         ByteArrayHelper.HexStringToByteArray(signature),
    //         ByteArrayHelper.HexStringToByteArray(message),
    //         ByteArrayHelper.HexStringToByteArray(publicKey));
    //     ed25519VerifyResult.ShouldBe(true);
    //     
    //     var contractContext1 = new CSharpSmartContractContext(bridgeContext);
    //     var publicKey1 = "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a";
    //     var message1 = "1";
    //     var signature1 = "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b";
    //     Should.Throw<ArgumentOutOfRangeException>(() => contractContext1.Ed25519Verify(
    //         ByteArrayHelper.HexStringToByteArray(signature1),
    //         ByteArrayHelper.HexStringToByteArray(message1),
    //         ByteArrayHelper.HexStringToByteArray(publicKey1)));
    //     
    // }
    
    [Fact]
    public void Verify_Transaction_Origin_SetValue()
    {
        var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
        var origin = SampleAddress.AddressList[0];
        bridgeContext.TransactionContext = new TransactionContext
        {
            Origin = origin,
            Transaction = new Transaction
            {
                From = SampleAddress.AddressList[1],
                To = SampleAddress.AddressList[2]
            }
        };
        var contractContext = new CSharpSmartContractContext(bridgeContext);
        contractContext.Origin.ShouldBe(origin);
        contractContext.Origin.ShouldNotBe(bridgeContext.TransactionContext.Transaction.From);
    }

    [Fact]
    public void Transaction_VerifySignature_Test()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        var transaction = new Transaction
        {
            From = Address.FromPublicKey(keyPair.PublicKey),
            To = SampleAddress.AddressList[2],
            MethodName = "Test",
            RefBlockNumber = 100
        };
        var signature = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);

        var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
        var result = bridgeContext.VerifySignature(transaction);
        result.ShouldBeTrue();
    }

    // [Fact]
    // public void keccak256_test()
    // {
    //     var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
    //     var origin = SampleAddress.AddressList[0];
    //     var contractContext = new CSharpSmartContractContext(bridgeContext);
    //     byte[] message = System.Text.Encoding.UTF8.GetBytes("Test message");
    //     var fact = contractContext.Keccak256(message);
    //     var expected = Sha3Keccack.Current.CalculateHash(message);
    //     fact.ShouldBe(expected);
    // }
    // public static byte[] ToBytes32(BigIntValue value)
    // {
    //     var bytes = value.ToBigEndianBytes();
    //     var newArray = new byte[32];
    //     for (int i = 0; i < bytes.Length; i++)
    //     {
    //         newArray[31 - i] = bytes[bytes.Length - 1 - i];
    //     }
    //
    //     return newArray;
    // }
    //
    // [Fact]
    // public void Bn254G1Mul_Test()
    // {
    //     var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
    //     var contractContext = new CSharpSmartContractContext(bridgeContext);
    //
    //     // Arrange
    //     byte[] x1 = ToBytes32(new BigIntValue(0));
    //     byte[] y1 = ToBytes32(new BigIntValue(0));
    //     byte[] scalar = ToBytes32(new BigIntValue(0));
    //
    //     // use raw api to compute result
    //     var (expectedXUInt256, expectedYUInt256) = Bn254.Net.Bn254.Mul(
    //         UInt256.FromBigEndianBytes(x1),
    //         UInt256.FromBigEndianBytes(y1),
    //         UInt256.FromBigEndianBytes(scalar)
    //     );
    //     var expectedX = expectedXUInt256.ToBigEndianBytes();
    //     var expectedY = expectedYUInt256.ToBigEndianBytes();
    //
    //     // Act
    //     var (xResult, yResult) = contractContext.Bn254G1Mul(x1, y1, scalar);
    //
    //     // Assert
    //     xResult.ShouldBe(expectedX);
    //     yResult.ShouldBe(expectedY);
    // }
    //
    // [Fact]
    // public void Bn254G1Add_Test()
    // {
    //     var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
    //     var contractContext = new CSharpSmartContractContext(bridgeContext);
    //
    //     // Arrange
    //     byte[] x1 = ToBytes32(new BigIntValue(0));
    //     byte[] y1 = ToBytes32(new BigIntValue(0));
    //     byte[] x2 = ToBytes32(new BigIntValue(0));
    //     byte[] y2 = ToBytes32(new BigIntValue(0));
    //
    //     // Use raw API to compute expected results
    //     var (expectedX3UInt256, expectedY3UInt256) = Bn254.Net.Bn254.Add(
    //         UInt256.FromBigEndianBytes(x1),
    //         UInt256.FromBigEndianBytes(y1),
    //         UInt256.FromBigEndianBytes(x2),
    //         UInt256.FromBigEndianBytes(y2)
    //     );
    //     var expectedX3 = expectedX3UInt256.ToBigEndianBytes();
    //     var expectedY3 = expectedY3UInt256.ToBigEndianBytes();
    //
    //     // Act
    //     var (x3Result, y3Result) = contractContext.Bn254G1Add(x1, y1, x2, y2);
    //
    //     // Assert
    //     x3Result.ShouldBe(expectedX3);
    //     y3Result.ShouldBe(expectedY3);
    // }
    //
    //
    // [Fact]
    // public void Bn254Pairing_Test()
    // {
    //     var bridgeContext = GetRequiredService<IHostSmartContractBridgeContextService>().Create();
    //     var contractContext = new CSharpSmartContractContext(bridgeContext);
    //
    //     // Arrange
    //     var input = new (byte[], byte[], byte[], byte[], byte[], byte[])[]
    //     {
    //         (
    //             ToBytes32(new BigIntValue(0)),
    //             ToBytes32(new BigIntValue(0)),
    //             ToBytes32(new BigIntValue(0)),
    //             ToBytes32(new BigIntValue(0)),
    //             ToBytes32(new BigIntValue(0)),
    //             ToBytes32(new BigIntValue(0))
    //         )
    //     };
    //
    //     // Use raw API to compute expected results
    //     bool expected = Bn254.Net.Bn254.Pairing(new (UInt256, UInt256, UInt256, UInt256, UInt256, UInt256)[]
    //     {
    //         (
    //             UInt256.FromBigEndianBytes(input[0].Item1),
    //             UInt256.FromBigEndianBytes(input[0].Item2),
    //             UInt256.FromBigEndianBytes(input[0].Item3),
    //             UInt256.FromBigEndianBytes(input[0].Item4),
    //             UInt256.FromBigEndianBytes(input[0].Item5),
    //             UInt256.FromBigEndianBytes(input[0].Item6)
    //         )
    //     });
    //
    //     // Act
    //     bool result = contractContext.Bn254Pairing(input);
    //
    //     // Assert
    //     result.ShouldBe(expected);
    // }
}