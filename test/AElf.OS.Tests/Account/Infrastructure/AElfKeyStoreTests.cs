using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.Exceptions;
using AElf.Kernel;
using AElf.OS.Node.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Account.Infrastructure
{
    public class AElfKeyStoreTests : KeyStoreTestBase
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
            var list = await _keyStore.GetAccountsAsync();
            list.Count.ShouldBeGreaterThanOrEqualTo(1);
            list.ShouldContain(Address.FromPublicKey(keyPair.PublicKey).ToBase58());

            var address = Address.FromPublicKey(keyPair.PublicKey);
            var addString = address.ToBase58();
            address.ShouldNotBe(null);

            //Open account
            var errResult = await _keyStore.UnlockAccountAsync(addString, "12");
            errResult.ShouldBe(AccountError.WrongPassword);

            errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AccountError.None);

            errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AccountError.AccountAlreadyUnlocked);

            errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AccountError.AccountAlreadyUnlocked);
        }

        [Fact]
        public async Task Account_Create_And_Read()
        {
            await Should.ThrowAsync<KeyStoreNotFoundException>(() => _keyStore.ReadKeyPairAsync("file", "123"));

            //Create
            var keyPair = await _keyStore.CreateAccountKeyPairAsync("123");
            keyPair.ShouldNotBe(null);
            var address = Address.FromPublicKey(keyPair.PublicKey);
            var publicKey = keyPair.PublicKey.ToHex();
            var addString = address.ToBase58();

            //Read
            await Should.ThrowAsync<KeyStoreNotFoundException>(() =>
                _keyStore.ReadKeyPairAsync(addString + "_fake", "123"));

            await Should.ThrowAsync<InvalidPasswordException>(() =>
                _keyStore.ReadKeyPairAsync(addString, "WrongPassword"));

            var keyPair1 = await _keyStore.ReadKeyPairAsync(addString, "123");
            var address1 = Address.FromPublicKey(keyPair1.PublicKey);
            var publicKey1 = keyPair1.PublicKey.ToHex();

            keyPair.PrivateKey.ShouldBe(keyPair1.PrivateKey);

            publicKey.ShouldBe(publicKey1);
            address.ShouldBe(address1);
        }

        [Fact]
        public async Task Open_NotExist_Account()
        {
            var address = SampleAddress.AddressList[0];
            var addString = address.ToBase58();
            var keyPair = _keyStore.GetAccountKeyPair(addString);
            keyPair.ShouldBeNull();

            var errResult = await _keyStore.UnlockAccountAsync(addString, "123");
            errResult.ShouldBe(AccountError.AccountFileNotFound);
        }

        [Fact]
        public async Task GetAccountKeyPair_Test()
        {
            var keyPair = await _keyStore.CreateAccountKeyPairAsync("123");
            var address = Address.FromPublicKey(keyPair.PublicKey).ToBase58();

            var result = _keyStore.GetAccountKeyPair(address);
            result.ShouldBeNull();

            await _keyStore.UnlockAccountAsync(address, "123");
            result = _keyStore.GetAccountKeyPair(address);
            result.ShouldNotBeNull();
            result.PublicKey.ShouldBe(keyPair.PublicKey);
        }

        public override void Dispose()
        {
            base.Dispose();
            DeleteTestFolder();
        }

        private void DeleteTestFolder()
        {
            var path = Path.Combine(_nodeEnvironmentService.GetAppDataPath(), "keys");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}