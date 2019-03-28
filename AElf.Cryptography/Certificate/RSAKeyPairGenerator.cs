﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace AElf.Cryptography.Certificate
{
    public class RSAKeyPairGenerator
    {
        public RSAKeyPair Generate()
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(new SecureRandom(), 2028));
            var kp = generator.GenerateKeyPair();
            return new RSAKeyPair(kp.Private, kp.Public);
        }
    }
}