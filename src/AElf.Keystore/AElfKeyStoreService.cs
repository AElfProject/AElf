using AElf.Cryptography;
using AElf.Types;
using Nethereum.KeyStore;

namespace AElf.KeyStore;

public class AElfKeyStoreService : KeyStoreService
{
    
    public string EncryptKeyStoreAsJson(string password, string privateKey)
    {
        var keyPair = CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey));
        var address = Address.FromPublicKey(keyPair.PublicKey);
        return EncryptAndGenerateDefaultKeyStoreAsJson(password, keyPair.PrivateKey, address.ToBase58());
    }
    
    public byte[] DecryptKeyStoreFromFile(string password, string filePath)
    {
        using var file = File.OpenText(filePath);
        var json = file.ReadToEnd();
        return DecryptKeyStoreFromJson(password, json);
    }
    
}