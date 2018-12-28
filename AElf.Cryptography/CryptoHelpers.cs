using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AElf.Cryptography.ECDSA;
using Secp256k1Net;

namespace AElf.Cryptography
{
    public static class CryptoHelpers
    {
        private static readonly Secp256k1 Secp256K1 = new Secp256k1();

        // ReaderWriterLock for thread-safe with Secp256k1 APIs
        private static readonly ReaderWriterLock Lock = new ReaderWriterLock();

        // TODO: maybe need refactor, both Cryptography.EC* and Cryptography.CryptoHelpers expose public method.
        static CryptoHelpers()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, arg) => { Secp256K1.Dispose(); };
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

                Secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey);
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                Secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey);
                return new ECKeyPair(privateKey, pubKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while GenerateKeyPair", ex);
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
                Secp256K1.SignRecoverable(recSig, hash, privateKey);
                Secp256K1.RecoverableSignatureSerializeCompact(compactSig, out var recoverId, recSig);
                compactSig[64] = (byte) recoverId; // put recover id at the last slot
                return compactSig;
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while SignWithPrivateKey", ex);
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static byte[] RecoverPublicKey(byte[] signature, byte[] hash)
        {
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                var recoveredPubKey = new byte[Secp256k1.PUBKEY_LENGTH];
                var recSig = new byte[65];
                Secp256K1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
                Secp256K1.Recover(recoveredPubKey, recSig, hash);
                Secp256K1.PublicKeySerialize(pubKey, recoveredPubKey);
                return pubKey;
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while RecoverPublicKey", ex);
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static bool Verify(byte[] signature, byte[] hash, byte[] pubKey)
        {
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var recSig = new byte[Secp256k1.UNSERIALIZED_SIGNATURE_SIZE];
                Secp256K1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
                return Secp256K1.Verify(recSig, hash, pubKey);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while Verify", ex);
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        public static bool Verify(byte[] signature, byte[] hash)
        {
            try
            {
                Lock.AcquireWriterLock(Timeout.Infinite);
                var publicKeyOutput = new byte[Secp256k1.PUBKEY_LENGTH];
                var recSig = new byte[65];
                Secp256K1.RecoverableSignatureParseCompact(recSig, signature, signature.Last());
                Secp256K1.Recover(publicKeyOutput, recSig, hash);
                return Secp256K1.Verify(recSig, hash, publicKeyOutput);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while Verify", ex);
            }
            finally
            {
                Lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Returns a byte array of the specified length, filled with random bytes.
        /// </summary>
        public static byte[] RandomFill(int count)
        {
            var rnd = new Random();
            var random = new byte[count];
            rnd.NextBytes(random);
            return random;
        }
    }
}