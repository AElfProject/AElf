using System.Security.Cryptography;
using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class KeyPairGenerator
    {
        public ECKeyPair Generate()
        {
            var privateKey = new byte[32];
            var publicKey = new byte[64];
            
            using (var secp256k1 = new Secp256k1())
            {
                // Generate a private key.
                var rnd = RandomNumberGenerator.Create();
                
                do
                {
                    rnd.GetBytes(privateKey);
                }
                while (!secp256k1.SecretKeyVerify(privateKey));
                
                secp256k1.PublicKeyCreate(publicKey, privateKey);
            }
            
            ECKeyPair k = new ECKeyPair(privateKey, publicKey);
        
            return k;
        }
    }
}