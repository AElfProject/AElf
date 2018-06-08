using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;

namespace AElf.Cryptography.ECDSA
{
    public class ECSigner
    {
        public ECSignature Sign(ECKeyPair keyPair, byte[] data)
        {
            ECDsaSigner ecdsaSigner = new ECDsaSigner();
            ecdsaSigner.Init(true, new ParametersWithRandom(keyPair.PrivateKey, Parameters.SecureRandom));
        
            BigInteger[] signature = ecdsaSigner.GenerateSignature(data);
            
            return new ECSignature(signature);
        }
    }
}