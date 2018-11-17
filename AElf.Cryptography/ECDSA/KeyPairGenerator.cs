using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace AElf.Cryptography.ECDSA
{
    public class KeyPairGenerator
    {
        public ECKeyPair Generate()
        {
            ECKeyGenerationParameters keygenParams 
                = new ECKeyGenerationParameters(ECParameters.DomainParams, ECParameters.SecureRandom);
        
            ECKeyPairGenerator generator = new ECKeyPairGenerator();
            generator.Init(keygenParams);
        
            AsymmetricCipherKeyPair keypair = generator.GenerateKeyPair();
        
            ECPrivateKeyParameters privParams = (ECPrivateKeyParameters)keypair.Private;
            ECPublicKeyParameters pubParams = (ECPublicKeyParameters)keypair.Public;
        
            ECKeyPair k = new ECKeyPair(privParams, pubParams);
        
            return k;
        }
    }
}