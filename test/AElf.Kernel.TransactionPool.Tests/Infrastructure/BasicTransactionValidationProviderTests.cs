using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Virgil.Crypto;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class BasicTransactionValidationProviderTests : TransactionPoolTestBase
    {
        private readonly BasicTransactionValidationProvider _basicTransactionValidationProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ILocalEventBus _eventBus;

        public BasicTransactionValidationProviderTests()
        {
            _basicTransactionValidationProvider = GetRequiredService<BasicTransactionValidationProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task ValidateTransaction_Test()
        {
            TransactionValidationStatusChangedEvent eventData = null;
            _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
            {
                eventData = d;
                return Task.CompletedTask;
            });

            var transaction = _kernelTestHelper.GenerateTransaction();
            var result =
                await _basicTransactionValidationProvider.ValidateTransactionAsync(transaction, new ChainContext());
            result.ShouldBeTrue();
            eventData.ShouldBeNull();

            transaction.Signature = ByteString.Empty;
            result =
                await _basicTransactionValidationProvider.ValidateTransactionAsync(transaction, new ChainContext());
            result.ShouldBeFalse();
            eventData.ShouldNotBeNull();
            eventData.TransactionId.ShouldBe(transaction.GetHash());
            eventData.TransactionResultStatus.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            eventData.Error.ShouldBe("Incorrect transaction signature.");

            eventData = null;
            transaction = GenerateBigTransaction();
            result =
                await _basicTransactionValidationProvider.ValidateTransactionAsync(transaction, new ChainContext());
            result.ShouldBeFalse();
            eventData.ShouldNotBeNull();
            eventData.TransactionId.ShouldBe(transaction.GetHash());
            eventData.TransactionResultStatus.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            eventData.Error.ShouldBe("Transaction size exceeded.");
        }

        private Transaction GenerateBigTransaction()
        {
            var transaction = new Transaction
            {
                From = Address.FromPublicKey(_kernelTestHelper.KeyPair.PublicKey),
                To = SampleAddress.AddressList[0],
                MethodName = Guid.NewGuid().ToString(),
                Params = ByteString.CopyFrom(new byte[TransactionPoolConsts.TransactionSizeLimit]),
                RefBlockNumber = 0,
                RefBlockPrefix = ByteString.Empty
            };

            var signature = CryptoHelper.SignWithPrivateKey(_kernelTestHelper.KeyPair.PrivateKey,
                transaction.GetHash().ToByteArray());
            transaction.Signature = ByteString.CopyFrom(signature);
            return transaction;
        }
    }
}