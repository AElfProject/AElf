using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECSigner
    {
        public ECSignature Sign(ECKeyPair keyPair, byte[] data)
        {
            var signature = new byte[65];
            using (var secp256k1 = new Secp256k1())
            {
                secp256k1.SignRecoverable(signature, data, keyPair.PrivateKey);
            }
            
            return new ECSignature(signature);
        }
    }
}