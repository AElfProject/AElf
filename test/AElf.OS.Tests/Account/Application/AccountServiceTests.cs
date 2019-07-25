using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.Account.Application
{
    public class AccountServiceTests : OSTestBase
    {
        private readonly AccountOptions _accountOptions;
        private readonly AccountService _accountService;

        public AccountServiceTests()
        {
            _accountOptions = GetRequiredService<IOptionsSnapshot<AccountOptions>>().Value;
            _accountService = GetRequiredService<AccountService>();
        }

        [Fact]
        public async Task GetPublicKeyTest()
        {
            var publicKey = await _accountService.GetPublicKeyAsync();

            Assert.Equal(Address.FromPublicKey(publicKey).GetFormatted(), _accountOptions.NodeAccount);
        }

        [Fact]
        public async Task GetAccountTest()
        {
            var account = await _accountService.GetAccountAsync();

            Assert.Equal(account.GetFormatted(), _accountOptions.NodeAccount);
        }

        [Fact]
        public async Task SignAndVerifyPassTest()
        {
            var data = Hash.FromString("test").ToByteArray();

            var signature = await _accountService.SignAsync(data);
            var publicKey = await _accountService.GetPublicKeyAsync();
            
            var recoverResult = CryptoHelper.RecoverPublicKey(signature, data, out var recoverPublicKey);
            var verifyResult =  recoverResult && publicKey.BytesEqual(recoverPublicKey);

            Assert.True(verifyResult);
        }

        [Fact]
        public async Task SignAndVerifyNotPassTest()
        {
            var data1 = Hash.FromString("test1").ToByteArray();
            var data2 = Hash.FromString("test2").ToByteArray();

            var signature = await _accountService.SignAsync(data1);
            var publicKey = await _accountService.GetPublicKeyAsync();

            var recoverResult = CryptoHelper.RecoverPublicKey(signature, data2, out var recoverPublicKey);
            var verifyResult =  recoverResult && publicKey.BytesEqual(recoverPublicKey);

            Assert.False(verifyResult);
        }

        [Fact]
        public async Task EncryptAndDecryptMessage()
        {
            var stringValue = new StringValue
            {
                Value = "EncryptAndDecryptMessage"
            };
            var pubicKey = await _accountService.GetPublicKeyAsync();
            var plainMessage = stringValue.ToByteArray();

            var encryptMessage = await _accountService.EncryptMessageAsync(pubicKey, plainMessage);

            var decryptMessage = await _accountService.DecryptMessageAsync(pubicKey, encryptMessage);

            decryptMessage.ShouldBe(plainMessage);
        }
    }
}
