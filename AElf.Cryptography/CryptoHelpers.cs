using System;
using System.Linq;
using System.Security.Cryptography;
using AElf.Cryptography.ECDSA;
using Secp256k1Net;

namespace AElf.Cryptography
{
    public static class CryptoHelpers
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once IdentifierTypo
        private static readonly Secp256k1 _secp256k1 = new Secp256k1();

        static CryptoHelpers()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, arg) => { _secp256k1.Dispose(); };
        }

        public static ECKeyPair GenerateKeyPair()
        {
            var privateKey = new byte[32];
            var _secp256K1PubKey = new byte[64];

            // Generate a private key.
            var rnd = RandomNumberGenerator.Create();

            do
            {
                rnd.GetBytes(privateKey);
            } while (!_secp256k1.SecretKeyVerify(privateKey));

            _secp256k1.PublicKeyCreate(_secp256K1PubKey, privateKey);

            var pubkey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];

            _secp256k1.PublicKeySerialize(pubkey, _secp256K1PubKey);

            return new ECKeyPair(privateKey, pubkey);
        }

        public static byte[] SignWithPrivateKey(byte[] privateKey, byte[] hash)
        {
            var recSig = new byte[65];
            var compactSig = new byte[65];

            _secp256k1.SignRecoverable(recSig, hash, privateKey);
            _secp256k1.RecoverableSignatureSerializeCompact(compactSig, out var recoverId, recSig);
            compactSig[64] = (byte) recoverId; // put recover id at the last slot

            return compactSig;
        }

        public static byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            byte[] recoveredPubKey = new byte[Secp256k1.PUBKEY_LENGTH];
            var recSig = new byte[65];
            _secp256k1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
            _secp256k1.Recover(recoveredPubKey, recSig, hash);

            return recoveredPubKey;
        }

        public static bool Verify(byte[] signature, byte[] hash, byte[] pubKey)
        {
            var recSig = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
            _secp256k1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
            return _secp256k1.Verify(recSig, hash, pubKey);
        }

        /// <summary>
        /// Returns a byte array of the specified length, filled with random bytes.
        /// </summary>
        public static byte[] RandomFill(int count)
        {
            Random rnd = new Random();
            byte[] random = new byte[count];

            rnd.NextBytes(random);

            return random;
        }
    }
}