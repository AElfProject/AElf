using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA.Exceptions;
using AElf.Kernel;
using AElf.OS.Node.Application;
using AElf.Types;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Account.Infrastructure
{
    public class AElfKeyStoreTests:KeyStoreTestBase
    {
        private readonly AElfKeyStore _keyStore;
        private readonly INodeEnvironmentService _nodeEnvironmentService;

        public AElfKeyStoreTests()
        {
            _keyStore = GetRequiredService<AElfKeyStore>();
            _nodeEnvironmentService = GetRequiredService<INodeEnvironmentService>();
        }

        [Fact]
        public async Task Account_Create_And_Open()
        {
            var keyPair = await _keyStore.CreateAccountKeyPairAsync("123");
            keyPair.ShouldNotBe(null);
            _keyStore.GetAccountsAsync().Result.Count.ShouldBeGreaterThanOrEqualTo(1);

            var address = Address.FromPublicKey(keyPair.PublicKey);
            var addString = address.GetFormatted();
            address.ShouldNotBe(null);

            //Open account
            var errResult = await _keyStore.UnlockAccountAsync(addString, "12", true);
            errResult.ShouldBe(AElfKeyStore.Errors.WrongPassword);

            errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AElfKeyStore.Errors.None);

            errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AElfKeyStore.Errors.AccountAlreadyUnlocked);

            errResult = await _keyStore.UnlockAccountAsync(addString, "123", false);
            errResult.ShouldBe(AElfKeyStore.Errors.AccountAlreadyUnlocked);

            Directory.Delete(Path.Combine(_nodeEnvironmentService.GetAppDataPath(), "keys"), true);

            await Should.ThrowAsync<KeyStoreNotFoundException>(() => _keyStore.ReadKeyPairAsync(addString + "_fake", "123"));
        }

        [Fact]
        public async Task Account_Create_And_Read_Compare()
        {
            for (var i = 0; i < 1000; i++)
            {
                //Create
                var keyPair = await _keyStore.CreateAccountKeyPairAsync("123");
                keyPair.ShouldNotBe(null);
                var address = Address.FromPublicKey(keyPair.PublicKey);
                var publicKey = keyPair.PublicKey.ToHex();
                var addString = address.GetFormatted();

                //Read
                var keyPair1 = await _keyStore.ReadKeyPairAsync(addString, "123");
                var address1 = Address.FromPublicKey(keyPair1.PublicKey);
                var publicKey1 = keyPair1.PublicKey.ToHex();

                keyPair.PrivateKey.ShouldBe(keyPair1.PrivateKey);

                publicKey.ShouldBe(publicKey1);
                address.ShouldBe(address1);

                Directory.Delete(Path.Combine(_nodeEnvironmentService.GetAppDataPath(), "keys"), true);
            }
        }

        [Fact]
        public async Task Open_NotExist_Account()
        {
            var address = SampleAddress.AddressList[0];
            var addString = address.GetFormatted();
            var keyPair = _keyStore.GetAccountKeyPair(addString);
            keyPair.ShouldBe(null);

            var errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AElfKeyStore.Errors.AccountFileNotFound);
        }

        [Fact]
        public async Task Open_Account_WithTimeout()
        {
            var keyPair = await _keyStore.CreateAccountKeyPairAsync("123");
            var address = Address.FromPublicKey(keyPair.PublicKey);
            var addString = address.GetFormatted();

            //Open account with timeout
            _keyStore.DefaultTimeoutToClose = TimeSpan.FromMilliseconds(50);
            await _keyStore.UnlockAccountAsync(addString, "123");
            
            Thread.Sleep(200); //update due to window ci io speed issue may cased case failed.
            var keyPairInfo = _keyStore.GetAccountKeyPair(addString);
            keyPairInfo.ShouldBeNull();
        }
    }
}