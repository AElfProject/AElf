using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.ECDSA.Exceptions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Cryptography.Tests
{
    public class AElfKeyStoreTests
    {
        private AElfKeyStore _keyStore;

        public AElfKeyStoreTests(ITestOutputHelper testOutputHelper)
        {
            InitKeyStore();
        }

        private void InitKeyStore()
        {
            _keyStore = new AElfKeyStore("/tmp");
        }

        [Fact]
        public async Task Account_Create_And_Open()
        {
            var keyPair = await _keyStore.CreateAsync("123", "AELF");
            keyPair.ShouldNotBe(null);
            _keyStore.ListAccountsAsync().Result.Count.ShouldBeGreaterThanOrEqualTo(1);

            var address = Address.FromPublicKey(keyPair.PublicKey);
            var addString = address.GetFormatted();
            address.ShouldNotBe(null);

            //Open account
            var errResult = await _keyStore.OpenAsync(addString, "12", true);
            errResult.ShouldBe(AElfKeyStore.Errors.WrongPassword);

            errResult = await _keyStore.OpenAsync(addString, "123");
            errResult.ShouldBe(AElfKeyStore.Errors.None);

            errResult = await _keyStore.OpenAsync(addString, "123");
            errResult.ShouldBe(AElfKeyStore.Errors.AccountAlreadyUnlocked);

            errResult = await _keyStore.OpenAsync(addString, "123", false);
            errResult.ShouldBe(AElfKeyStore.Errors.AccountAlreadyUnlocked);

            Directory.Delete("/tmp/keys", true);

            await Should.ThrowAsync<KeyStoreNotFoundException>(() => _keyStore.ReadKeyPairAsync(addString + "_fake", "123"));
        }

        [Fact]
        public async Task Account_Create_And_Read_Compare()
        {
            for (var i = 0; i < 1000; i++)
            {
                //Create
                var keyPair = await _keyStore.CreateAsync("123", "AELF");
                keyPair.ShouldNotBe(null);
                var address = Address.FromPublicKey(keyPair.PublicKey);
                var publicKey = keyPair.PublicKey.ToHex();
                var addString = address.GetFormatted();

                //Read
                var keyPair1 = await _keyStore.ReadKeyPairAsync(addString, "123");
                var address1 = Address.FromPublicKey(keyPair1.PublicKey);
                var publicKey1 = keyPair1.PublicKey.ToHex();

                // keyPair.PrivateKey.ShouldBe(keyPair1.PrivateKey);

                publicKey.ShouldBe(publicKey1);
                address.ShouldBe(address1);

                Directory.Delete("/tmp/keys", true);
            }
        }

        [Fact]
        public async Task Open_NotExist_Account()
        {
            var address = Address.FromString("test account");
            var addString = address.GetFormatted();
            var keyPair = _keyStore.GetAccountKeyPair(addString);
            keyPair.ShouldBe(null);

            var errResult = await _keyStore.OpenAsync(addString, "123");
            errResult.ShouldBe(AElfKeyStore.Errors.AccountFileNotFound);
        }
    }
}