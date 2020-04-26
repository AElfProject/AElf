using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Blockchain.Application;
using AElf.TestBase;
using Shouldly;
using Xunit;


namespace AElf.Kernel.Account.Application
{
    public class AccountServiceTests: AElfIntegratedTest<KernelCoreAccountServiceTestAElfModule>

    {
        private readonly IAccountService _accountService;

        public AccountServiceTests()
        {
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async void Encrypt_Decrypt_Message_Test()
        {
            var publicKey = await _accountService.GetPublicKeyAsync();
            var msg = new byte[] {1, 2, 3, 4, 5};
            var encryptMsg = await _accountService.EncryptMessageAsync(publicKey, msg);
            var decryptMsg = await _accountService.DecryptMessageAsync(publicKey, encryptMsg);
            decryptMsg.ShouldBe(msg);
        }
    }
}