using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Infrastructure;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Account.Application;

[Trait("Category", AElfAccountModule)]
public sealed class AccountServiceTests : AccountTestBase
{
    private readonly IAccountService _accountService;
    private readonly ECKeyPair _ecKeyPair;

    public AccountServiceTests()
    {
        _accountService = GetRequiredService<IAccountService>();
        _ecKeyPair = CryptoHelper.GenerateKeyPair();
        var aelfAsymmetricCipherKeyPairProvider = GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();
        aelfAsymmetricCipherKeyPairProvider.SetKeyPair(_ecKeyPair);
    }

    [Fact]
    public async Task GetPublicKey_Test()
    {
        var publicKey = await _accountService.GetPublicKeyAsync();
        publicKey.ShouldBe(_ecKeyPair.PublicKey);
    }

    [Fact]
    public async Task GetAccount_Test()
    {
        var address = await _accountService.GetAccountAsync();
        address.ShouldBe(Address.FromPublicKey(_ecKeyPair.PublicKey));
    }

    [Fact]
    public async Task Sign_Test()
    {
        var data = HashHelper.ComputeFrom("test").ToByteArray();
        var signature = await _accountService.SignAsync(data);

        var recoverResult = CryptoHelper.RecoverPublicKey(signature, data, out var recoverPublicKey);
        var verifyResult = recoverResult && _ecKeyPair.PublicKey.BytesEqual(recoverPublicKey);
        verifyResult.ShouldBeTrue();
    }

    [Fact]
    public async Task EncryptAndDecryptMessage_Test()
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