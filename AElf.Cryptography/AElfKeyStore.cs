using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECDSA.Exceptions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace AElf.Cryptography
{
    public class AElfKeyStore //: IKeyStore
    {
        private static readonly SecureRandom _random = new SecureRandom();

        private const string _algo = "AES-256-CFB";
        private string _keyStorePath;
        
        public bool IsOpen { get; private set; }

        private List<OpenAccount> _openAccounts;
        
        private TimeSpan _defaultAccountTimeout = TimeSpan.FromMinutes(5);
        
        public AElfKeyStore(string keyStorePath)
        {
            _keyStorePath = keyStorePath;
            _openAccounts = new List<OpenAccount>();
        }

        public Task OpenAsync(string address, string password)
        {
            //OpenAccount acc = new OpenAccount();
            //Timer t = new Timer(RemoveAccount, acc, TimeSpan.Zero, _defaultAccountTimeout);
            
            return Task.CompletedTask;
        }

        private void RemoveAccount(object accObj)
        {
            if (accObj is OpenAccount openAccount)
            {
            }
        }

        public ECKeyPair ReadKeyPairAsync(string address, string password)
        {
            try
            {
                string baseDir = Path.Combine(_keyStorePath, "aelf", "keys");
                string kpFile = Path.Combine(baseDir, address);
                string filePath = Path.ChangeExtension(kpFile, ".ak");

                AsymmetricCipherKeyPair p;
                using (var textReader = File.OpenText(filePath))
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
                throw new InvalidPasswordException("Invalid password.", pemEx);
            }
            catch (Exception e)
            {
                ;
            }

            return null;
        }

        public void WriteKeyPair(ECKeyPair keyPair, string password)
        {
            try
            {
                var akp = new AsymmetricCipherKeyPair(keyPair.PublicKey, keyPair.PrivateKey);

                string baseDir = Path.Combine(_keyStorePath, "aelf", "keys");

                Directory.CreateDirectory(baseDir);
                    
                string kpFile = Path.Combine(baseDir, BitConverter.ToString(keyPair.GetEncodedPublicKey().Take(10).ToArray()).Replace("-",""));
                string filePath = Path.ChangeExtension(kpFile, ".ak");
                
                using (TextWriter writer = File.CreateText(filePath))
                {
                    PemWriter pw = new PemWriter(writer);

                    pw.WriteObject(akp, _algo, password.ToCharArray(), _random);
                    pw.Writer.Close();

                    writer.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }


}