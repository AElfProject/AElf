using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Secp256k1Net;

namespace AElf.Cryptography.ECDSA
{
    public class ECKeyPair : IAElfAsymmetricCipherKeyPair
    {
        public byte[] PrivateKey { get; }
        public byte[] PublicKey { get; }

        internal ECKeyPair(byte[] privateKey, byte[] publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey.LeftPad(Secp256k1.PRIVKEY_LENGTH);
        }
    }
}