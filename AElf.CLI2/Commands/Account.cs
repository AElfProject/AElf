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

            using (var fs = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof(EncryptedAccount));
                serializer.Serialize(fs, this);
            }
        }

        public static EncryptedAccount LoadFromFile(string path)
        {
            EncryptedAccount acc = new EncryptedAccount();
            using (var fs = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof(EncryptedAccount));
                return (EncryptedAccount) serializer.Deserialize(fs);
            }
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