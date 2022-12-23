using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract;

public class HostSmartContractBridgeContextTests : SmartContractRunnerTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
    private readonly KernelTestHelper _kernelTestHelper;
    private readonly ECKeyPair _keyPair;
    private readonly ISmartContractAddressService _smartContractAddressService;

    private IHostSmartContractBridgeContext _bridgeContext;

    public HostSmartContractBridgeContextTests()
    {
        _blockchainService = GetRequiredService<IBlockchainService>();
        _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        _defaultContractZeroCodeProvider = GetRequiredService<IDefaultContractZeroCodeProvider>();
        _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        _keyPair = CryptoHelper.GenerateKeyPair();
        _bridgeContext = CreateNewContext();
    }

    [Fact]
    public void Recover_Context_PublicKey_Success()
    {
        var recoverPublicKey = _bridgeContext.RecoverPublicKey();
        recoverPublicKey.ShouldNotBeNull();
        recoverPublicKey.ShouldBe(_keyPair.PublicKey);
    }

    [Fact]
    public void Send_Inline_Success()
    {
        var to = SampleAddress.AddressList[0];
        var methodName = "TestSendInline";
        var arg = "Arg";
        var argBytes = new StringValue { Value = arg }.ToByteString();
        _bridgeContext.SendInline(to, methodName, argBytes);

        var inlineTransaction = _bridgeContext.TransactionContext.Trace.InlineTransactions;
        inlineTransaction.Count.ShouldBe(1);
        inlineTransaction[0].From.ShouldBe(_bridgeContext.Self);
        inlineTransaction[0].To.ShouldBe(to);
        inlineTransaction[0].MethodName.ShouldBe(methodName);
        inlineTransaction[0].Params.ShouldBe(argBytes);
    }

    [Fact]
    public void Send_VirtualInline_Success()
    {
        var from = HashHelper.ComputeFrom("hash");
        var to = SampleAddress.AddressList[0];
        var methodName = "TestVirtualInline";
        var arg = "Arg";
        var argBytes = new StringValue { Value = arg }.ToByteString();
        _bridgeContext.SendVirtualInline(from, to, methodName, argBytes);

        var inlineTransaction = _bridgeContext.TransactionContext.Trace.InlineTransactions;
        inlineTransaction.Count.ShouldBe(1);
        inlineTransaction[0].From.ShouldNotBe(_bridgeContext.Self);
        inlineTransaction[0].To.ShouldBe(to);
        inlineTransaction[0].MethodName.ShouldBe(methodName);
        inlineTransaction[0].Params.ShouldBe(argBytes);
    }

    [Fact]
    public void Get_GetPreviousTransactions_Success()
    {
        var transaction = GetNewTransaction();

        var newBlock = _kernelTestHelper.GenerateBlock(0, Hash.Empty, new List<Transaction> { transaction });

        _blockchainService.AddTransactionsAsync(new List<Transaction> { transaction });
        _blockchainService.AddBlockAsync(newBlock);

        _bridgeContext.TransactionContext.PreviousBlockHash = newBlock.GetHash();

        var previousBlockTransactions = _bridgeContext.GetPreviousBlockTransactions();

        previousBlockTransactions.ShouldNotBeNull();
        previousBlockTransactions.ShouldContain(transaction);
    }

    [Fact]
    public void Verify_Signature_NoSignature_ReturnFalse()
    {
        var tx = new Transaction();
        var verifyResult = _bridgeContext.VerifySignature(tx);
        verifyResult.ShouldBe(false);
    }

    [Fact]
    public void Verify_Signature_SingleSignature_ReturnTrue()
    {
        var tx = new Transaction
        {
            From = Address.FromPublicKey(_keyPair.PublicKey),
            To = SampleAddress.AddressList[0],
            MethodName = "TestMethod",
            Params = ByteString.CopyFrom(new byte[10]),
            RefBlockNumber = 1,
            RefBlockPrefix = ByteString.CopyFrom(new byte[4])
        };
        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().ToByteArray());
        tx.Signature = ByteString.CopyFrom(signature);

        var verifyResult = _bridgeContext.VerifySignature(tx);
        verifyResult.ShouldBe(true);
    }

    [Fact]
    public void Verify_Signature_SingleSignature_ReturnFalse()
    {
        var tx = GetNewTransaction();

        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().ToByteArray());
        tx.Signature = ByteString.CopyFrom(signature);

        var verifyResult = _bridgeContext.VerifySignature(tx);
        verifyResult.ShouldBe(false);
    }

    [Fact]
    public void Deploy_Contract_ThrowNoPermissionException()
    {
        Should.Throw<NoPermissionException>(() =>
            _bridgeContext.DeployContract(_smartContractAddressService.GetZeroSmartContractAddress(),
                new SmartContractRegistration(), null));
    }

    [Fact]
    public void Deploy_Contract_Success()
    {
        _bridgeContext.TransactionContext.Transaction = new Transaction
        {
            To = _smartContractAddressService.GetZeroSmartContractAddress()
        };

        var registration = new SmartContractRegistration
        {
            Category = KernelConstants.DefaultRunnerCategory,
            Code = ByteString.Empty,
            CodeHash = HashHelper.ComputeFrom("hash")
        };

        _bridgeContext.DeployContract(SampleAddress.AddressList[0], registration,
            HashHelper.ComputeFrom(registration.CodeHash));
    }

    [Fact]
    public void Update_Contract_Success()
    {
        _bridgeContext.TransactionContext.Transaction = new Transaction
        {
            To = _smartContractAddressService.GetZeroSmartContractAddress()
        };

        var registration = new SmartContractRegistration
        {
            Category = KernelConstants.DefaultRunnerCategory,
            Code = ByteString.Empty,
            CodeHash = Hash.Empty
        };

        _bridgeContext.UpdateContract(SampleAddress.AddressList[0], registration, null);
    }

    [Fact]
    public void Update_Contract_WithoutPermission()
    {
        _bridgeContext.TransactionContext.Transaction = new Transaction
        {
            To = SampleAddress.AddressList[0]
        };

        var registration = new SmartContractRegistration
        {
            Category = KernelConstants.DefaultRunnerCategory,
            Code = ByteString.Empty,
            CodeHash = Hash.Empty
        };

        Should.Throw<NoPermissionException>(() =>
        {
            _bridgeContext.UpdateContract(SampleAddress.AddressList[0], registration, null);
        });
    }

    [Fact]
    public void Test_OriginTransactionId()
    {
        var hostSmartContractBridgeContext = CreateNewContext();
        hostSmartContractBridgeContext.OriginTransactionId.ShouldBe(_bridgeContext.TransactionContext
            .OriginTransactionId);
        hostSmartContractBridgeContext.OriginTransactionId.ShouldBe(_bridgeContext.TransactionContext.Transaction
            .GetHash());
    }

    [Fact]
    public void Test_GenerateId()
    {
        var hostSmartContractBridgeContext = CreateNewContext();
        Should.NotThrow(() => hostSmartContractBridgeContext.GenerateId(SampleAddress.AddressList[0], null));
        var bytes = new List<byte> { 0x01 };
        Should.Throw<NullReferenceException>(() => hostSmartContractBridgeContext.GenerateId(null, bytes));
        var res1 = hostSmartContractBridgeContext.GenerateId(SampleAddress.AddressList[0], bytes);
        var res2 = hostSmartContractBridgeContext.GenerateId(SampleAddress.AddressList[0], bytes.ToArray());
        res1.ShouldBe(res2);
    }

    [Fact]
    public void Recover_PublicKey_Success()
    {
        var data = HashHelper.ComputeFrom("test data");
        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, data.ToByteArray());
        var publicKey = _bridgeContext.RecoverPublicKey(signature, data.ToByteArray());
        publicKey.ShouldBe(_keyPair.PublicKey);
    }
    
    [Fact]
    public void Recover_PublicKey_Failed()
    {
        var data = HashHelper.ComputeFrom("test data");
        var incorrectData = HashHelper.ComputeFrom("incorrect data");
        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, data.ToByteArray());
        var publicKey = _bridgeContext.RecoverPublicKey(signature, incorrectData.ToByteArray());
        publicKey.ShouldNotBe(_keyPair.PublicKey);
    }

    private IHostSmartContractBridgeContext CreateNewContext()
    {
        _bridgeContext = GetRequiredService<IHostSmartContractBridgeContext>();

        var transaction = new Transaction
        {
            From = Address.FromPublicKey(_keyPair.PublicKey),
            To = SampleAddress.AddressList[0],
            MethodName = "Test",
            Params = ByteString.CopyFrom(new byte[10]),
            RefBlockNumber = 1,
            RefBlockPrefix = ByteString.CopyFrom(new byte[4])
        };
        var transactionContext = new TransactionContext
        {
            Transaction = transaction,
            BlockHeight = 3,
            CurrentBlockTime = TimestampHelper.GetUtcNow(),
            PreviousBlockHash = Hash.Empty,
            Trace = new TransactionTrace(),
            StateCache = new NullStateCache(),
            OriginTransactionId = transaction.GetHash()
        };
        var signature = CryptoHelper.SignWithPrivateKey(_keyPair.PrivateKey, transactionContext.Transaction
            .GetHash().ToByteArray());
        transactionContext.Transaction.Signature = ByteString.CopyFrom(signature);
        _bridgeContext.Initialize(transactionContext);

        return _bridgeContext;
    }

    private Transaction GetNewTransaction()
    {
        var tx = new Transaction
        {
            From = SampleAddress.AddressList[0],
            To = SampleAddress.AddressList[1],
            MethodName = Guid.NewGuid().ToString(),
            Params = ByteString.CopyFrom(new byte[10]),
            RefBlockNumber = 1,
            RefBlockPrefix = ByteString.CopyFrom(new byte[4])
        };
        return tx;
    }
}