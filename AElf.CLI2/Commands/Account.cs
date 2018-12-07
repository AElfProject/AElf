using System;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace AElf.CLI2.Commands
{
    public struct Account
    {
        public string Mnemonic;
        public string PrivateKey;
        public string Address;
    }

    public struct EncryptedAccount
    {
        public string EncryptedMnemonic;
        public string EncryptedPrivateKey;
        public string Address;

        public void DumpToFile(string path)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($@"Account file ""{path}"" already exists.");
                return;
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
        }

        public static EncryptedAccount LoadFromFile(string path)
        {
            return JsonConvert.DeserializeObject<EncryptedAccount>(File.ReadAllText(path));
        }

        public Account Decrypt(string password)
        {
            var acc = new Account();
            if (!string.IsNullOrEmpty(EncryptedMnemonic))
            {
                acc.Mnemonic = Utils.Cryptography.Decrypt(EncryptedMnemonic, password);
            }

            if (!string.IsNullOrEmpty(EncryptedPrivateKey))
            {
                acc.PrivateKey = Utils.Cryptography.Decrypt(EncryptedPrivateKey, password);
            }

            return acc;
        }
    }
}