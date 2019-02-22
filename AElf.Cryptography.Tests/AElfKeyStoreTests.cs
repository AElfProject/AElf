using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.ECDSA.Exceptions;
using Xunit;
using Shouldly;

namespace AElf.Cryptography.Tests
{
    public class AElfKeyStoreTests
    {
        private AElfKeyStore _keyStore;

        public AElfKeyStoreTests()
        {
            InitKeyStore();
        }

        private void InitKeyStore()
        {
            _keyStore = new AElfKeyStore("/tmp");
        }

        [Fact]
        public void Account_Create_And_Open()
        {
            var keyPair = _keyStore.CreateAsync("123", "AELF").Result;
            keyPair.ShouldNotBe(null);
            _keyStore.ListAccountsAsync().Result.Count.ShouldBeGreaterThanOrEqualTo(1);

            var address = Address.FromPublicKey(keyPair.PublicKey);
            string addString = address.GetFormatted();
            address.ShouldNotBe(null);
            addString.StartsWith("ELF").ShouldBe(true);

            var keyPair1 = _keyStore.ReadKeyPairAsync(addString, "123").Result;
            keyPair1.PrivateKey.ShouldBe(keyPair.PrivateKey);
            keyPair.PublicKey.ShouldBe(keyPair.PublicKey);

            //Open account
            var errResult = _keyStore.OpenAsync(addString, "12", true).Result;
            errResult.ShouldBe(AElfKeyStore.Errors.WrongPassword);

            errResult = _keyStore.OpenAsync(addString, "123").Result;
            errResult.ShouldBe(AElfKeyStore.Errors.None);

            errResult = _keyStore.OpenAsync(addString, "123").Result;
            errResult.ShouldBe(AElfKeyStore.Errors.AccountAlreadyUnlocked);

            errResult = _keyStore.OpenAsync(addString, "123", false).Result;
            errResult.ShouldBe(AElfKeyStore.Errors.AccountAlreadyUnlocked);

            Directory.Delete("/tmp/keys", true);
            Should.ThrowAsync<KeyStoreNotFoundException>(() => { return _keyStore.ReadKeyPairAsync(addString + "_fake", "123"); });
        }

        [Fact]
        public void Open_NotExist_Account()
        {
            var address = Address.FromString("test account");
            var addString = address.GetFormatted();
            var keyPair = _keyStore.GetAccountKeyPair(addString);
            keyPair.ShouldBe(null);

            var errResult = _keyStore.OpenAsync(addString, "123").Result;
            errResult.ShouldBe(AElfKeyStore.Errors.AccountFileNotFound);
        }
    }
}