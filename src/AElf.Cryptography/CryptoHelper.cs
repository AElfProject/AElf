using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AElf.Cryptography.Core;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.ECVRF;
using AElf.Cryptography.Exceptions;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Secp256k1Net;
using ECParameters = AElf.Cryptography.ECDSA.ECParameters;

namespace AElf.Cryptography
{

    public static class CryptoHelper
    {
        private static readonly Secp256k1 Secp256K1 = new Secp256k1();

        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();  

        private static readonly Vrf<Secp256k1Curve, Sha256HasherFactory> Vrf =
            new Vrf<Secp256k1Curve, Sha256HasherFactory>(new VrfConfig(0xfe, ECParameters.Curve));

        static CryptoHelper()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, arg) => { Secp256K1.Dispose(); };
        }

        public static ECKeyPair FromPrivateKey(byte[] privateKey)
        {
            if (privateKey == null || privateKey.Length != 32)
                throw new InvalidPrivateKeyException(
                    $"Private key has to have length of 32. Current length is {privateKey?.Length}.");

            try
            {
                Lock.EnterWriteLock();
                var secp256K1PubKey = new byte[64];

                if (!Secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey))
                    throw new InvalidPrivateKeyException("Create public key failed.");
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if (!Secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey))
                    throw new PublicKeyOperationException("Serialize public key failed.");
                return new ECKeyPair(privateKey, pubKey);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static ECKeyPair GenerateKeyPair()
        {
            try
            {
                Lock.EnterWriteLock();
                var privateKey = new byte[32];
                var secp256K1PubKey = new byte[64];

                // Generate a private key.
                var rnd = RandomNumberGenerator.Create();
                do
                {
                    rnd.GetBytes(privateKey);
                } while (!Secp256K1.SecretKeyVerify(privateKey));

                if (!Secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey))
                    throw new InvalidPrivateKeyException("Create public key failed.");
                var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if (!Secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey))
                    throw new PublicKeyOperationException("Serialize public key failed.");
                return new ECKeyPair(privateKey, pubKey);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static byte[] SignWithPrivateKey(byte[] privateKey, byte[] hash)
        {
            try
            {
                Lock.EnterWriteLock();
                var recSig = new byte[65];
                var compactSig = new byte[65];
                if (!Secp256K1.SignRecoverable(recSig, hash, privateKey))
                    throw new SignatureOperationException("Create a recoverable ECDSA signature failed.");
                if (!Secp256K1.RecoverableSignatureSerializeCompact(compactSig, out var recoverId, recSig))
                    throw new SignatureOperationException("Serialize an ECDSA signature failed.");
                compactSig[64] = (byte)recoverId; // put recover id at the last slot
                return compactSig;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static bool VerifySignature(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = RecoverPublicKey(signature, data, out var recoverPublicKey);
            return recoverResult && publicKey.BytesEqual(recoverPublicKey);
        }

        public static bool RecoverPublicKey(byte[] signature, byte[] hash, out byte[] pubkey)
        {
            pubkey = null;
            try
            {
                Lock.EnterWriteLock();
                // Recover id should be greater than or equal to 0 and less than 4
                if (signature.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH || signature.Last() >= 4)
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
                pubkey = pubKey;
                return true;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static byte[] EncryptMessage(byte[] senderPrivateKey, byte[] receiverPublicKey, byte[] plainText)
        {
            var keyBytes = GetSharedSecret(senderPrivateKey, receiverPublicKey);

            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
            cipher.Init(true, new ParametersWithIV(new KeyParameter(keyBytes), new byte[16]));

            var cipherText = new byte[cipher.GetOutputSize(plainText.Length)];
            var len = cipher.ProcessBytes(plainText, 0, plainText.Length, cipherText, 0);
            cipher.DoFinal(cipherText, len);

            return cipherText;
        }

        public static byte[] DecryptMessage(byte[] senderPublicKey, byte[] receiverPrivateKey, byte[] cipherText)
        {
            var keyBytes = GetSharedSecret(receiverPrivateKey, senderPublicKey);

            var cipher = new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()));
            cipher.Init(false, new ParametersWithIV(new KeyParameter(keyBytes), new byte[16]));

            var temp = new byte[cipher.GetOutputSize(cipherText.Length)];
            var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, temp, 0);
            len += cipher.DoFinal(temp, len);

            // Remove padding
            var plainText = new byte[len];
            Array.Copy(temp, 0, plainText, 0, len);

            return plainText;
        }

        private static byte[] GetSharedSecret(byte[] privateKey, byte[] publicKey)
        {
            var curve = ECParameters.Curve;
            var domainParams = ECParameters.DomainParams;
            var privateKeyParams = new ECPrivateKeyParameters(new BigInteger(1, privateKey), domainParams);
            var publicKeyParams = new ECPublicKeyParameters(curve.Curve.DecodePoint(publicKey), domainParams);

            var agreement = AgreementUtilities.GetBasicAgreement("ECDH");
            agreement.Init(privateKeyParams);
            var secret = agreement.CalculateAgreement(publicKeyParams);

            var kdf = new Kdf2BytesGenerator(new Sha256Digest());
            kdf.Init(new KdfParameters(secret.ToByteArray(), null));

            var keyBytes = new byte[32];
            kdf.GenerateBytes(keyBytes, 0, keyBytes.Length);

            return keyBytes;
        }

        public static byte[] ECVrfProve(ECKeyPair keyPair, byte[] alpha)
        {
            try
            {
                Lock.EnterWriteLock();
                return Vrf.Prove(keyPair, alpha);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }

        public static byte[] ECVrfVerify(byte[] publicKey, byte[] alpha, byte[] pi)
        {
            try
            {
                Lock.EnterWriteLock();
                return Vrf.Verify(publicKey, alpha, pi);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
    }
}