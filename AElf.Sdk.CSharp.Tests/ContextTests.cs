using System;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Sdk.CSharp.Tests
{
    public class ContextTests : SdkCSharpTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractService _smartContractService;
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ECKeyPair _keyPair;

        public ContextTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractService = GetRequiredService<ISmartContractService>();
            _defaultContractZeroCodeProvider = GetRequiredService<IDefaultContractZeroCodeProvider>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _keyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public void Recover_PublicKey_Success()
        {
            var context = CreateNewContext();

            var hash = Hash.FromString("RecoverPublicKey").DumpByteArray();
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, hash);

            var recoverPublicKey = context.RecoverPublicKey(signature, hash);
            recoverPublicKey.ShouldNotBeNull();
            recoverPublicKey.ShouldBe(_keyPair.PublicKey);
        }

        [Fact]
        public void Recover_PublicKey_Fail()
        {
            var context = CreateNewContext();

            var hash = Hash.FromString("RecoverPublicKey").DumpByteArray();
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey,
                Hash.FromString("RecoverPublicKeyFail").DumpByteArray());

            var recoverPublicKey = context.RecoverPublicKey(signature, hash);
            recoverPublicKey.ShouldNotBe(_keyPair.PublicKey);
        }

        [Fact]
        public void Recover_Context_PublicKey_Success()
        {
            var context = CreateNewContext();

            var recoverPublicKey = context.RecoverPublicKey();
            recoverPublicKey.ShouldNotBeNull();
            recoverPublicKey.ShouldBe(_keyPair.PublicKey);
        }

        [Fact]
        public void Send_Inline_Success()
        {
            var context = CreateNewContext();

            var to = Address.Genesis;
            var methodName = "TestSendInline";
            var arg = "Arg";
            context.SendInline(to, methodName, arg);

            var inlineTransaction = context.TransactionContext.Trace.InlineTransactions;
            inlineTransaction.Count.ShouldBe(1);
            inlineTransaction[0].From.ShouldBe(context.Self);
            inlineTransaction[0].To.ShouldBe(to);
            inlineTransaction[0].MethodName.ShouldBe(methodName);
            inlineTransaction[0].Params.ShouldBe(ByteString.CopyFrom(ParamsPacker.Pack(arg)));
        }

        [Fact]
        public void Get_GetPreviousBlock_Success()
        {
            var context = CreateNewContext();

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

            context.TransactionContext.PreviousBlockHash = newBlock.GetHash();

            var previousBlock = context.GetPreviousBlock();
            previousBlock.Height.ShouldBe(newBlock.Height);
            previousBlock.GetHash().ShouldBe(newBlock.GetHash());
        }

        [Fact]
        public void Verify_Signature_NoSignature_ReturnFalse()
        {
            var context = new Context();

            var tx = new Transaction();
            var verifyResult = context.VerifySignature(tx);
            verifyResult.ShouldBe(false);
        }

        [Fact]
        public void Verify_Signature_SingleSignature_ReturnTrue()
        {
            var context = new Context();

            var tx = new Transaction
            {
                From = Address.FromPublicKey(_keyPair.PublicKey),
                To = Address.FromString("To"),
                MethodName = "TestMethod"
            };
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            var verifyResult = context.VerifySignature(tx);
            verifyResult.ShouldBe(true);
        }

        [Fact]
        public void Verify_Signature_SingleSignature_ReturnFalse()
        {
            var context = new Context();

            var tx = GetNewTransaction();

            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            var verifyResult = context.VerifySignature(tx);
            verifyResult.ShouldBe(false);
        }

        [Fact]
        public void Verify_Signature_MultiSignature_ReturnTrue()
        {
            var context = new Context();

            var tx = GetNewTransaction();

            var signature1 =
                CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, Hash.FromString("Signature1").DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature1));
            var signature2 =
                CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, Hash.FromString("Signature2").DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature2));

            var verifyResult = context.VerifySignature(tx);
            verifyResult.ShouldBe(true);
        }

        [Fact]
        public void Send_DeferredTransaction_Success()
        {
            var context = CreateNewContext();

            var deferredTransaction = GetNewTransaction();

            context.SendDeferredTransaction(deferredTransaction);

            var currentDeferredTransaction = context.TransactionContext.Trace.DeferredTransaction;
            currentDeferredTransaction.ShouldBe(deferredTransaction.ToByteString());
        }

        [Fact]
        public void Deploy_Contract_ThrowAssertionError()
        {
            var context = CreateNewContext();
            Assert.Throws<AssertionError>(() =>
                context.DeployContract(_smartContractAddressService.GetZeroSmartContractAddress(),
                    new SmartContractRegistration(), null));
        }

        [Fact]
        public void Deploy_Contract_Success()
        {
            var context = CreateNewContext();
            var smartContractContext = new SmartContractContext
            {
                ContractAddress = _defaultContractZeroCodeProvider.ContractZeroAddress,
                BlockchainService = _blockchainService,
                SmartContractService = _smartContractService,
                SmartContractAddressService = _smartContractAddressService
            };
            context.SmartContractContext = smartContractContext;

            var registration = new SmartContractRegistration
            {
                Category = 0,
                Code = ByteString.Empty,
                CodeHash = Hash.Zero
            };

            context.DeployContract(Address.Zero, registration, null);
        }

        [Fact]
        public void Update_Contract_ThrowAssertionError()
        {
            var context = CreateNewContext();
            Assert.Throws<AssertionError>(() =>
                context.UpdateContract(_smartContractAddressService.GetZeroSmartContractAddress(),
                    new SmartContractRegistration(), null));
        }

        [Fact]
        public void Update_Contract_Success()
        {
            var context = CreateNewContext();
            var smartContractContext = new SmartContractContext
            {
                ContractAddress = _defaultContractZeroCodeProvider.ContractZeroAddress,
                BlockchainService = _blockchainService,
                SmartContractService = _smartContractService,
                SmartContractAddressService = _smartContractAddressService
            };
            context.SmartContractContext = smartContractContext;

            var registration = new SmartContractRegistration
            {
                Category = 0,
                Code = ByteString.Empty,
                CodeHash = Hash.Zero
            };

            context.UpdateContract(Address.Zero, registration, null);
        }

        private Context CreateNewContext()
        {
            var context = new Context();

            var smartContractContext = new SmartContractContext
            {
                ContractAddress = Address.Genesis,
                BlockchainService = _blockchainService,
                SmartContractService = _smartContractService,
                SmartContractAddressService = _smartContractAddressService
            };
            context.SmartContractContext = smartContractContext;

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
                Trace = new TransactionTrace()
            };
            var signature = CryptoHelpers.SignWithPrivateKey(_keyPair.PrivateKey, transactionContext.Transaction
                .GetHash().DumpByteArray());
            transactionContext.Transaction.Sigs.Add(ByteString.CopyFrom(signature));
            context.TransactionContext = transactionContext;

            return context;
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