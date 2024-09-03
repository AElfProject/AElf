using System;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using CustomContract = AElf.Runtime.CSharp.Tests.TestContract;

namespace AElf.Sdk.CSharp.Tests;

public class CSharpSmartContractContextTests : SdkCSharpTestBase
{
    [Fact]
    public void Verify_Ed25519Verify()
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
        var publicKey = "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a";
        var message = "";
        var signature = "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b";
        var ed25519VerifyResult = contractContext.Ed25519Verify(
            ByteArrayHelper.HexStringToByteArray(signature),
            ByteArrayHelper.HexStringToByteArray(message),
            ByteArrayHelper.HexStringToByteArray(publicKey));
        ed25519VerifyResult.ShouldBe(true);
        
        var contractContext1 = new CSharpSmartContractContext(bridgeContext);
        var publicKey1 = "d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a";
        var message1 = "1";
        var signature1 = "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b";
        Should.Throw<ArgumentOutOfRangeException>(() => contractContext1.Ed25519Verify(
            ByteArrayHelper.HexStringToByteArray(signature1),
            ByteArrayHelper.HexStringToByteArray(message1),
            ByteArrayHelper.HexStringToByteArray(publicKey1)));
        
        // var contractContext2 = new CSharpSmartContractContext(bridgeContext);
        // var publicKey2 = "3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c";
        // var message2 = "72";
        // var signature2 = "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00";
        // var ed25519VerifyResult2 = contractContext2.Ed25519Verify(
        //     ByteArrayHelper.HexStringToByteArray(signature2),
        //     ByteArrayHelper.HexStringToByteArray(message2),
        //     ByteArrayHelper.HexStringToByteArray(publicKey2));
        // ed25519VerifyResult2.ShouldBe(true);
        
    }
    
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
}