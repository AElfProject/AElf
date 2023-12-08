// ReSharper disable StringLiteralTypo

using Xunit;

namespace AElf.KeyStore.Tests;

public class AElfKeyStoreServiceTests
{
    private const string Password = "abcde";
    private const string PrivateKeyHex = "ff96c3463af0b8629f170f078f97ac0147490b92e1784e3bff93f7ee9d1abcb6";
    
    [Fact]
    public void EncryptKeyStoreAsJson_Test()
    {
        var keystoreService = new AElfKeyStoreService();
        var keystoreJson = keystoreService.EncryptKeyStoreAsJson(
            Password,
            PrivateKeyHex
        );
        Assert.Contains("\"address\":\"VQFq9atg4fMtFLhqpVh48ZnhX8FXMGBHW8MDANPpCSHcZisU6\"", keystoreJson);
        var privateKey = keystoreService.DecryptKeyStoreFromJson(Password, keystoreJson);
        Assert.Equal(ByteArrayHelper.HexStringToByteArray(PrivateKeyHex), privateKey);
    }

    [Fact]
    public void DecryptKeyStoreFromJson_Test()
    {
        const string jsonKeyStore =
            """
            {
                "crypto": {
                    "cipher": "aes-128-ctr",
                    "ciphertext": "1734a897caeea53e306fa6908fca443e598ea1cb6361fb27e5c45de61a26eb25",
                    "cipherparams": {
                        "iv": "36f00104f0488386db1f3e3d37b14bfe"
                    },
                    "kdf": "scrypt",
                    "mac": "011ff2bee41f29f8fb6f7a63b1266bf04054bf1881e94cd3a77ead034ec47892",
                    "kdfparams": {
                        "n": 262144,
                        "r": 1,
                        "p": 8,
                        "dklen": 32,
                        "salt": "fd09ae033a9660eb1ad6a0bb0bf7b03ee30944c2fe621eef2a262fd3d2a92881"
                    }
                },
                "id": "7b2aa039-291d-4e09-8f18-8d503fd7711a",
                "address": "VQFq9atg4fMtFLhqpVh48ZnhX8FXMGBHW8MDANPpCSHcZisU6",
                "version": 3
            }
            """;
        var keystoreService = new AElfKeyStoreService();
        var privateKey = keystoreService.DecryptKeyStoreFromJson(Password, jsonKeyStore);
        Assert.Equal(ByteArrayHelper.HexStringToByteArray(PrivateKeyHex), privateKey);
    }
    
    [Fact]
    public void DecryptKeyStoreFromFile_Test()
    {
        var keystoreService = new AElfKeyStoreService();
        var privateKey = keystoreService.DecryptKeyStoreFromFile(Password, "data/test.json");
        Assert.Equal(ByteArrayHelper.HexStringToByteArray(PrivateKeyHex), privateKey);
    }
    
}