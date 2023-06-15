using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.OS.Account.Application;

public class AccountServiceTests : AccountServiceTestBase
{
    private readonly AccountOptions _accountOptions;
    private readonly IAccountService _accountService;

    public AccountServiceTests()
    {
        _accountOptions = GetRequiredService<IOptionsSnapshot<AccountOptions>>().Value;
        _accountService = GetRequiredService<IAccountService>();
    }

    [Fact]
    public async Task GetPublicKeyTest()
    {
        var publicKey = await _accountService.GetPublicKeyAsync();

        Assert.Equal(Address.FromPublicKey(publicKey).ToBase58(), _accountOptions.NodeAccount);

        // Test unlock account
        publicKey = await _accountService.GetPublicKeyAsync();
        Assert.Equal(Address.FromPublicKey(publicKey).ToBase58(), _accountOptions.NodeAccount);
    }

    [Fact]
    public async Task GetAccountTest()
    {
        var account = await _accountService.GetAccountAsync();

        Assert.Equal(account.ToBase58(), _accountOptions.NodeAccount);
    }

    [Fact]
    public async Task SignAndVerifyPassTest()
    {
        var data = HashHelper.ComputeFrom("test").ToByteArray();

        var signature = await _accountService.SignAsync(data);
        var publicKey = await _accountService.GetPublicKeyAsync();

        var recoverResult = CryptoHelper.RecoverPublicKey(signature, data, out var recoverPublicKey);
        var verifyResult = recoverResult && publicKey.BytesEqual(recoverPublicKey);

        Assert.True(verifyResult);
    }

    [Fact]
    public async Task SignAndVerifyNotPassTest()
    {
        var data1 = HashHelper.ComputeFrom("test1").ToByteArray();
        var data2 = HashHelper.ComputeFrom("test2").ToByteArray();

        var signature = await _accountService.SignAsync(data1);
        var publicKey = await _accountService.GetPublicKeyAsync();

        var recoverResult = CryptoHelper.RecoverPublicKey(signature, data2, out var recoverPublicKey);
        var verifyResult = recoverResult && publicKey.BytesEqual(recoverPublicKey);

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

    [Fact]
    public async Task GetAccountAsync_WithOptionEmpty()
    {
        _accountOptions.NodeAccount = string.Empty;
        // Test create key pair
        var account = await _accountService.GetAccountAsync();
        account.ShouldNotBeNull();

        // Test read key pair
        account = await _accountService.GetAccountAsync();
        account.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task Vrf_Test()
    {
        var alpha = "5cf8151010716e40e5349ad02821da605df22e9ac95450c7e35f04c720fd4db5";
        var alphaBytes = Hash.LoadFromHex(alpha).ToByteArray();
        var pi = await _accountService.ECVrfProveAsync(alphaBytes);
        var pubkey = await _accountService.GetPublicKeyAsync();
        var beta = CryptoHelper.ECVrfVerify(pubkey, alphaBytes, pi);
        beta.ShouldNotBeEmpty();
    }
}