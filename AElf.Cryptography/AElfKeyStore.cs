﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECDSA.Exceptions;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace AElf.Cryptography
{
    public class AElfKeyStore //: IKeyStore
    {
        private static readonly SecureRandom _random = new SecureRandom();

        private const string KeyFileExtension = ".ak";
        private const string KeyFolderName = "keys";

        private const string _algo = "AES-256-CFB";
        private readonly string _dataDirectory;
        
        public bool IsOpen { get; private set; }

        private readonly List<OpenAccount> _openAccounts;
        
        private TimeSpan _defaultAccountTimeout = TimeSpan.FromMinutes(10);

        public enum Errors
        {
            None = 0,
            AccountAlreadyUnlocked = 1,
            WrongPassword = 2
        }
        
        public AElfKeyStore(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
            _openAccounts = new List<OpenAccount>();
        }

        internal void OpenAsync(string address, string password, TimeSpan? timeout)
        {
            ECKeyPair kp = ReadKeyPairAsync(address, password);
            
            OpenAccount acc = new OpenAccount();
            acc.KeyPair = kp;

            if (timeout.HasValue)
            {
                Timer t = new Timer(RemoveAccount, acc, timeout.Value, timeout.Value);
                acc.Timer = t;
            }
            
            _openAccounts.Add(acc);
        }

        public Errors OpenAsync(string address, string password, bool withTimeout = true)
        {
            if (_openAccounts.Any(x => x.Address == address))
                return Errors.AccountAlreadyUnlocked;

            try
            {
                if (withTimeout)
                {
                    OpenAsync(address, password, _defaultAccountTimeout);
                }
                else
                {
                    OpenAsync(address, password, null);
                }
            }
            catch (InvalidPasswordException e)
            {
                return Errors.WrongPassword;
            }

            return Errors.None;
        }

        private void RemoveAccount(object accObj)
        {
            if (accObj is OpenAccount openAccount)
            {
                openAccount.Close();
                _openAccounts.Remove(openAccount);
            }
        }
        
        public ECKeyPair GetAccountKeyPair(string address)
        {
            return _openAccounts.FirstOrDefault(oa => oa.Address.Equals(address))?.KeyPair;
        }

        public ECKeyPair Create(string password)
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            bool res = WriteKeyPair(keyPair, password);
            return !res ? null : keyPair;
        }

        public List<string> ListAccounts()
        {
            var dir = GetOrCreateKeystoreDir();
            FileInfo[] files = dir.GetFiles("*" + KeyFileExtension);

            return files.Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToList();
        }

        public ECKeyPair ReadKeyPairAsync(string address, string password)
        {
            try
            {
                string keyFilePath = GetKeyFileFullPath(address);

                AsymmetricCipherKeyPair p;
                using (var textReader = File.OpenText(keyFilePath))
                {
                    PemReader pr = new PemReader(textReader, new Password(password.ToCharArray()));
                    p = pr.ReadObject() as AsymmetricCipherKeyPair;
                }

                if (p == null)
                    return null;
                
                ECKeyPair kp = new ECKeyPair((ECPrivateKeyParameters) p.Private, (ECPublicKeyParameters) p.Public);

                return kp;
            }
            catch (FileNotFoundException ex)
            {
                throw new KeyStoreNotFoundException("Keystore file not found.", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new KeyStoreNotFoundException("Invalid keystore path.", ex);
            }
            catch (PemException pemEx)
            {
                //Console.WriteLine("Invalid password.");
                throw new InvalidPasswordException("Invalid password", pemEx);
            }
            catch (Exception e)
            {
                ;
            }

            return null;
        }

        public bool WriteKeyPair(ECKeyPair keyPair, string password)
        {
            if (keyPair == null || keyPair.PrivateKey == null || keyPair.PublicKey == null)
                throw new InvalidKeyPairException("Invalid keypair (null reference).", null);

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Invalid password.", null);
                return false;
            }
                
            
            // Ensure path exists
            GetOrCreateKeystoreDir();
            
            string fullPath = null;
            try
            {
                var address = keyPair.GetAddressHex();
                fullPath = GetKeyFileFullPath(address);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not calculate the address from the keypair.", e);
                return false;
            }
            
            var akp = new AsymmetricCipherKeyPair(keyPair.PublicKey, keyPair.PrivateKey);
            
            using (StreamWriter writer = File.CreateText(fullPath))
            {
                PemWriter pw = new PemWriter(writer);

                pw.WriteObject(akp, _algo, password.ToCharArray(), _random);
                pw.Writer.Close();
            }

            return true;
        }
        
        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        internal string GetKeyFileFullPath(string address)
        {
            string dirPath = GetKeystoreDirectoryPath();
            string filePath = Path.Combine(dirPath, address);
            string filePathWithExtension = Path.ChangeExtension(filePath, KeyFileExtension);

            return filePathWithExtension;
        }

        internal DirectoryInfo GetOrCreateKeystoreDir()
        {
            try
            {
                string dirPath = GetKeystoreDirectoryPath();
                return Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                throw new KeyStoreNotFoundException("Invalid data directory path", e);
            }
        }

        internal string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_dataDirectory, KeyFolderName);
        }
    }
}