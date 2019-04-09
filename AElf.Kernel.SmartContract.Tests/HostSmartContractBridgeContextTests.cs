using System;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Security;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract
{
    public class HostSmartContractBridgeContextTests : SmartContractRunnerTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ECKeyPair _keyPair;

        private IHostSmartContractBridgeContext _bridgeContext;

        public HostSmartContractBridgeContextTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _defaultContractZeroCodeProvider = GetRequiredService<IDefaultContractZeroCodeProvider>();
            _keyPair = CryptoHelpers.GenerateKeyPair();
            _bridgeContext = CreateNewContext();
        }

        [Fact]
        public void Recover_PublicKey_Success()
        {
            var hash = Hash.FromString("RecoverPublicKey").DumpByteArray();
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey,
                Hash.FromString("RecoverPublicKeyFail").DumpByteArray());

            var recoverPublicKey = _bridgeContext.RecoverPublicKey(signature, hash);
            recoverPublicKey.ShouldNotBe(_keyPair.PublicKey);
        }

        [Fact]
        public void Recover_PublicKey_Fail()
        {
            var hash = Hash.FromString("RecoverPublicKey").DumpByteArray();
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey,
                Hash.FromString("RecoverPublicKeyFail").DumpByteArray());

            var recoverPublicKey = _bridgeContext.RecoverPublicKey(signature, hash);
            recoverPublicKey.ShouldNotBe(_keyPair.PublicKey);
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
            var to = Address.Genesis;
            var methodName = "TestSendInline";
            var arg = "Arg";
            var argBytes = new StringValue {Value = arg}.ToByteString();
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
            var from = Hash.Generate();
            var to = Address.Genesis;
            var methodName = "TestVirtualInline";
            var arg = "Arg";
            var argBytes = new StringValue {Value = arg}.ToByteString();
            _bridgeContext.SendVirtualInline(from, to, methodName, argBytes);

            var inlineTransaction = _bridgeContext.TransactionContext.Trace.InlineTransactions;
            inlineTransaction.Count.ShouldBe(1);
            inlineTransaction[0].From.ShouldNotBe(_bridgeContext.Self);
            inlineTransaction[0].To.ShouldBe(to);
            inlineTransaction[0].MethodName.ShouldBe(methodName);
            inlineTransaction[0].Params.ShouldBe(argBytes);
        }

        [Fact]
        public void Get_GetPreviousBlock_Success()
        {
            var newBlock = new Block
            {
                Height = 2,
                Header = new BlockHeader
                {
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            _blockchainService.AddBlockAsync(newBlock);

            _bridgeContext.TransactionContext.PreviousBlockHash = newBlock.GetHash();

            var previousBlock = _bridgeContext.GetPreviousBlock();
            previousBlock.Height.ShouldBe(newBlock.Height);
            previousBlock.GetHash().ShouldBe(newBlock.GetHash());
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
                To = Address.FromString("To"),
                MethodName = "TestMethod"
            };
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            var verifyResult = _bridgeContext.VerifySignature(tx);
            verifyResult.ShouldBe(true);
        }

        [Fact]
        public void Verify_Signature_SingleSignature_ReturnFalse()
        {
            var tx = GetNewTransaction();

            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            var verifyResult = _bridgeContext.VerifySignature(tx);
            verifyResult.ShouldBe(false);
        }

        [Fact]
        public void Verify_Signature_MultiSignature_ReturnTrue()
        {
            var tx = GetNewTransaction();

            var signature1 =
                CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, Hash.FromString("Signature1").DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature1));
            var signature2 =
                CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, Hash.FromString("Signature2").DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature2));

            var verifyResult = _bridgeContext.VerifySignature(tx);
            verifyResult.ShouldBe(true);
        }

        [Fact]
        public void Send_DeferredTransaction_Success()
        {
            var deferredTransaction = GetNewTransaction();

            _bridgeContext.SendDeferredTransaction(deferredTransaction);

            var currentDeferredTransaction = _bridgeContext.TransactionContext.Trace.DeferredTransaction;
            currentDeferredTransaction.ShouldBe(deferredTransaction.ToByteString());
        }

        [Fact]
        public void Deploy_Contract_ThrowNoPermissionException()
        {
            Should.Throw<NoPermissionException>(() =>
                _bridgeContext.DeployContract(_smartContractAddressService.GetZeroSmartContractAddress(),
                    new SmartContractRegistration(), null));
        }

        [Fact]
        public void Deploy_Contract_ThrowInvalidParameterException()
        {
            _bridgeContext.TransactionContext.Transaction = new Transaction()
            {
                To = _smartContractAddressService.GetZeroSmartContractAddress()
            };

            Should.Throw<InvalidParameterException>(() =>
                _bridgeContext.DeployContract(_smartContractAddressService.GetZeroSmartContractAddress(),
                    new SmartContractRegistration(){Category = -1}, null));
        }

        [Fact]
        public void Deploy_Contract_Success()
        {
            _bridgeContext.TransactionContext.Transaction = new Transaction()
            {
                To = _smartContractAddressService.GetZeroSmartContractAddress()
            };

            var registration = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.Empty,
                CodeHash = Hash.Generate()
            };

            _bridgeContext.DeployContract(Address.Zero, registration, Hash.FromMessage(registration.CodeHash));
        }

        [Fact]
        public void Update_Contract_ThrowAssertionError()
        {
            _bridgeContext.TransactionContext.Transaction = new Transaction()
            {
                To = _smartContractAddressService.GetZeroSmartContractAddress()
            };

            Should.Throw<InvalidParameterException>(() =>
                _bridgeContext.UpdateContract(_smartContractAddressService.GetZeroSmartContractAddress(),
                    new SmartContractRegistration(){Category = -1}, null));
        }

        [Fact]
        public void Update_Contract_Success()
        {
            _bridgeContext.TransactionContext.Transaction = new Transaction()
            {
                To = _smartContractAddressService.GetZeroSmartContractAddress()
            };

            var registration = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.Empty,
                CodeHash = Hash.Empty
            };

            _bridgeContext.UpdateContract(Address.Zero, registration, null);
        }
        
        private IHostSmartContractBridgeContext CreateNewContext()
        {
            _bridgeContext = GetRequiredService<IHostSmartContractBridgeContext>();

            var transactionContext = new TransactionContext()
            {
                Transaction = new Transaction()
                {
                    From = Address.FromPublicKey(_keyPair.PublicKey),
                    To = Address.Genesis
                },
                BlockHeight = 3,
                CurrentBlockTime = DateTime.Now,
                PreviousBlockHash = Hash.Empty,
                Trace = new TransactionTrace(),
                StateCache = new NullStateCache()
            };
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, transactionContext.Transaction
                .GetHash().DumpByteArray());
            transactionContext.Transaction.Sigs.Add(ByteString.CopyFrom(signature));
            _bridgeContext.TransactionContext = transactionContext;

            return _bridgeContext;
        }

        private Transaction GetNewTransaction()
        {
            var tx = new Transaction
            {
                From = Address.FromString("From"),
                To = Address.FromString("To"),
                MethodName = Guid.NewGuid().ToString()
            };
            return tx;
        }
    }
}