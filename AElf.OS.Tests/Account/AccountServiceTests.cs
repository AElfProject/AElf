using System.Threading.Tasks;
using AElf.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.OS.Account
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
            var data = Hash.FromString("test").DumpByteArray();

            var signature = await _accountService.SignAsync(data);
            var publicKey = await _accountService.GetPublicKeyAsync();
            var verifyResult = await _accountService.VerifySignatureAsync(signature, data, publicKey);

            Assert.True(verifyResult);
        }

        [Fact]
        public async Task SignAndVerifyNotPassTest()
        {
            var data1 = Hash.FromString("test1").DumpByteArray();
            var data2 = Hash.FromString("test2").DumpByteArray();

            var signature = await _accountService.SignAsync(data1);
            var publicKey = await _accountService.GetPublicKeyAsync();
            var verifyResult = await _accountService.VerifySignatureAsync(signature, data2, publicKey);

            Assert.False(verifyResult);
        }
    }
}