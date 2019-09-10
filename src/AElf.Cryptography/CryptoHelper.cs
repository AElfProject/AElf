using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using AElf.Cryptography.ECDSA;
using Secp256k1Net;
using Virgil.Crypto;

[assembly: InternalsVisibleTo("AElf.Cryptography.Tests")]

namespace AElf.Cryptography
{
    public static class CryptoHelper
    {
        private static readonly Secp256k1 Secp256K1 = new Secp256k1();

        private static readonly VirgilCrypto Crypto = new VirgilCrypto(KeyPairType.RSA_2048);

        // ReaderWriterLock for thread-safe with Secp256k1 APIs
        private static readonly ReaderWriterLock Lock = new ReaderWriterLock();

        static CryptoHelper()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, arg) => { Secp256K1.Dispose(); };
        }

        public static ECKeyPair FromPrivateKey(byte[] privateKey)
        {
            if (privateKey == null || privateKey.Length != 32)
            {
                throw new ArgumentException("Private key has to have length of 32.");
            }

            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var secp256K1PubKey = new byte[64];

                if(!Secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey))
                    throw new InvalidOperationException("Create public key failed.");
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if(!Secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey))
                    throw new InvalidOperationException("Serialize public key failed.");
                return new ECKeyPair(privateKey, pubKey);
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static ECKeyPair GenerateKeyPair()
        {
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var privateKey = new byte[32];
                var secp256K1PubKey = new byte[64];

                // Generate a private key.
                var rnd = RandomNumberGenerator.Create();
                do
                {
                    rnd.GetBytes(privateKey);
                } while (!Secp256K1.SecretKeyVerify(privateKey));

                if(!Secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey))
                    throw new InvalidOperationException("Create public key failed.");
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if(!Secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey))
                    throw new InvalidOperationException("Serialize public key failed.");
                return new ECKeyPair(privateKey, pubKey);
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static byte[] SignWithPrivateKey(byte[] privateKey, byte[] hash)
        {
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var recSig = new byte[65];
                var compactSig = new byte[65];
                if(!Secp256K1.SignRecoverable(recSig, hash, privateKey))
                    throw new InvalidOperationException("Create a recoverable ECDSA signature failed.");
                if(!Secp256K1.RecoverableSignatureSerializeCompact(compactSig, out var recoverId, recSig))
                    throw new InvalidOperationException("Serialize an ECDSA signature failed.");
                compactSig[64] = (byte) recoverId; // put recover id at the last slot
                return compactSig;
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static bool VerifySignature(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = RecoverPublicKey(signature, data, out var recoverPublicKey);
            return recoverResult && publicKey.BytesEqual(recoverPublicKey);
        }

        public static bool RecoverPublicKey(byte[] signature, byte[] hash, out byte[] publicKey)
        {
            publicKey = null;
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                if (signature.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
                    return false;
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                var recoveredPubKey = new byte[Secp256k1.PUBKEY_LENGTH];
                var recSig = new byte[65];
                if (!Secp256K1.RecoverableSignatureParseCompact(recSig, signature, signature.Last()))
                    return false;
                if (!Secp256K1.Recover(recoveredPubKey, recSig, hash))
                    return false;
                if (!Secp256K1.PublicKeySerialize(pubKey, recoveredPubKey))
                    return false;
                publicKey = pubKey;
                return true;
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static byte[] EncryptMessage(byte[] senderPrivateKey, byte[] receiverPublicKey, byte[] plainText)
        {
            var crypto = new VirgilCrypto(KeyPairType.EC_SECP256K1);
            var ecdhKey = Ecdh(senderPrivateKey, receiverPublicKey);
            var newKeyPair = crypto.GenerateKeys(KeyPairType.EC_SECP256K1, ecdhKey);
            return crypto.Encrypt(plainText, newKeyPair.PublicKey);
        }

        public static byte[] DecryptMessage(byte[] senderPublicKey, byte[] receiverPrivateKey, byte[] cipherText)
        {
            var crypto = new VirgilCrypto(KeyPairType.EC_SECP256K1);
            var ecdhKey = Ecdh(receiverPrivateKey, senderPublicKey);
            var newKeyPair = crypto.GenerateKeys(KeyPairType.EC_SECP256K1, ecdhKey);
            return crypto.Decrypt(cipherText, newKeyPair.PrivateKey);
        }

        public static byte[] Ecdh(byte[] privateKey, byte[] publicKey)
        {
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var usablePublicKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if(!Secp256K1.PublicKeyParse(usablePublicKey, publicKey))
                    throw new InvalidOperationException("Parse public key failed.");
                var ecdhKey = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
                if(!Secp256K1.Ecdh(ecdhKey, usablePublicKey, privateKey))
                    throw new InvalidOperationException("Compute EC Diffie- secret failed.");
                return ecdhKey;
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Returns a byte array of the specified length, filled with random bytes.
        /// </summary>
        internal static byte[] RandomFill(int count)
        {
            var rnd = new Random();
            var random = new byte[count];
            rnd.NextBytes(random);
            return random;
        }
    }
}