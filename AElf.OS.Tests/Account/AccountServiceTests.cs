using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.OS.Tests.Account
{
    public class AccountServiceTests : OSTestBase
    {
        private readonly AccountOptions _accountOptions;
        private readonly IAccountService _accountService;
        
        public AccountServiceTests()
        {
            _accountOptions = GetRequiredService<IOptionsSnapshot<AccountOptions>>().Value;
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public void GetPublicKeyTest()
        {
            var publicKey = _accountService.GetPublicKeyAsync().Result;

            Assert.Equal(Address.FromPublicKey(publicKey).GetFormatted(), _accountOptions.NodeAccount);
        }
        
        [Fact]
        public void GetAccountTest()
        {
            var account = _accountService.GetAccountAsync().Result;

            Assert.Equal(account.GetFormatted(), _accountOptions.NodeAccount);
        }

        [Fact]
        public void SignAndVerifyPassTest()
        {
            var data = Hash.FromString("test").DumpByteArray();

            var signature = _accountService.SignAsync(data).Result;
            var verifyResult = _accountService.VerifySignatureAsync(signature, data).Result;
            
            Assert.True(verifyResult);
        }

        [Fact]
        public async Task SignAndVerifyNotPassTest()
        {
            var data1 = Hash.FromString("test1").DumpByteArray();
            var data2 = Hash.FromString("test2").DumpByteArray();

            var signature = await _accountService.SignAsync(data1);
            var verifyResult = _accountService.VerifySignatureAsync(signature, data2).Result;
            
            Assert.False(verifyResult);
        }
    }
}