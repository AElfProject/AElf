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
        /*private static readonly Secp256k1 secp256k1 = new Secp256k1();

        static CryptoHelpers()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, arg) => { secp256k1.Dispose(); };
        }*/

        public static ECKeyPair GenerateKeyPair()
        {
            var privateKey = new byte[32];
            var secp256k1PubKey = new byte[64];

            // Generate a private key.
            var rnd = RandomNumberGenerator.Create();
            byte[] pubkey;
            using (Secp256k1 secp256k1 = new Secp256k1())
            {
                do
                {
                    rnd.GetBytes(privateKey);
                } while (!secp256k1.SecretKeyVerify(privateKey));

                secp256k1.PublicKeyCreate(secp256k1PubKey, privateKey);

                pubkey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];

                secp256k1.PublicKeySerialize(pubkey, secp256k1PubKey);
            }

            return new ECKeyPair(privateKey, pubkey);
        }

        public static byte[] SignWithPrivateKey(byte[] privateKey, byte[] hash)
        {
            var recSig = new byte[65];
            var compactSig = new byte[65];
            using (Secp256k1 secp256k1 = new Secp256k1())
            {
                secp256k1.SignRecoverable(recSig, hash, privateKey);
                secp256k1.RecoverableSignatureSerializeCompact(compactSig, out var recoverId, recSig);
                compactSig[64] = (byte) recoverId; // put recover id at the last slot
            }
            return compactSig;
        }

        public static byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            byte[] pubkey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            using (Secp256k1 secp256k1 = new Secp256k1())
            {
                byte[] recoveredPubKey;
                recoveredPubKey = new byte[Secp256k1.PUBKEY_LENGTH];
                var recSig = new byte[65];
                secp256k1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
                secp256k1.Recover(recoveredPubKey, recSig, hash);
                secp256k1.PublicKeySerialize(pubkey, recoveredPubKey);
            }
            return pubkey;
        }

        public static bool Verify(byte[] signature, byte[] hash, byte[] pubKey)
        {
            using (Secp256k1 secp256K1 = new Secp256k1())
            {
                var recSig = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                secp256K1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
                return secp256K1.Verify(recSig, hash, pubKey);
            }
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