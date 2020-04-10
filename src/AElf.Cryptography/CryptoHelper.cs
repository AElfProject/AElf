using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.Exceptions;
using Secp256k1Net;
using Virgil.Crypto;

namespace AElf.Cryptography
{
    public static class CryptoHelper
    {
        private static readonly ObjectPool<CryptoObject> CryptoObjects;

        static CryptoHelper()
        {
            CryptoObjects = new ObjectPool<CryptoObject>(() =>
            {
                var obj = new CryptoObject();
                AppDomain.CurrentDomain.ProcessExit += (sender, arg) => { obj.Dispose(); };
                return obj;
            });
        }


        public static ECKeyPair FromPrivateKey(byte[] privateKey)
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.FromPrivateKey(privateKey);
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }

        public static ECKeyPair GenerateKeyPair()
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.GenerateKeyPair();
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }

        public static byte[] SignWithPrivateKey(byte[] privateKey, byte[] hash)
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.SignWithPrivateKey(privateKey, hash);
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }

        public static bool VerifySignature(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = RecoverPublicKey(signature, data, out var recoverPublicKey);
            return recoverResult && publicKey.BytesEqual(recoverPublicKey);
        }

        public static bool RecoverPublicKey(byte[] signature, byte[] hash, out byte[] pubkey)
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.RecoverPublicKey(signature, hash, out pubkey);
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }

        public static byte[] EncryptMessage(byte[] senderPrivateKey, byte[] receiverPublicKey, byte[] plainText)
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.EncryptMessage(senderPrivateKey, receiverPublicKey, plainText);
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }

        public static byte[] DecryptMessage(byte[] senderPublicKey, byte[] receiverPrivateKey, byte[] cipherText)
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.DecryptMessage(senderPublicKey, receiverPrivateKey, cipherText);
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }

        public static byte[] Ecdh(byte[] privateKey, byte[] publicKey)
        {
            CryptoObject obj = CryptoObjects.GetObject();
            try
            {
                return obj.Ecdh(privateKey, publicKey);
            }
            finally
            {
                CryptoObjects.PutObject(obj);
            }
        }
    }


    public class ObjectPool<T>
    {
        private ConcurrentBag<T> _objects;
        private Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            T item;
            if (_objects.TryTake(out item)) return item;
            return _objectGenerator();
        }

        public void PutObject(T item)
        {
            _objects.Add(item);
        }
    }

    public class CryptoObject : IDisposable
    {
        private readonly Secp256k1 _secp256K1 = new Secp256k1();
        private readonly VirgilCrypto _crypto = new VirgilCrypto(KeyPairType.EC_SECP256K1);


        public ECKeyPair FromPrivateKey(byte[] privateKey)
        {
            if (privateKey == null || privateKey.Length != 32)
            {
                throw new InvalidPrivateKeyException(
                    $"Private key has to have length of 32. Current length is {privateKey?.Length}.");
            }

            var secp256K1PubKey = new byte[64];

            if (!_secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey))
                throw new InvalidPrivateKeyException("Create public key failed.");
            var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            if (!_secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey))
                throw new PublicKeyOperationException("Serialize public key failed.");
            return new ECKeyPair(privateKey, pubKey);
        }

        public ECKeyPair GenerateKeyPair()
        {
            var privateKey = new byte[32];
            var secp256K1PubKey = new byte[64];

            // Generate a private key.
            var rnd = RandomNumberGenerator.Create();
            do
            {
                rnd.GetBytes(privateKey);
            } while (!_secp256K1.SecretKeyVerify(privateKey));

            if (!_secp256K1.PublicKeyCreate(secp256K1PubKey, privateKey))
                throw new InvalidPrivateKeyException("Create public key failed.");
            var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            if (!_secp256K1.PublicKeySerialize(pubKey, secp256K1PubKey))
                throw new PublicKeyOperationException("Serialize public key failed.");
            return new ECKeyPair(privateKey, pubKey);
        }

        public byte[] SignWithPrivateKey(byte[] privateKey, byte[] hash)
        {
            var recSig = new byte[65];
            var compactSig = new byte[65];
            if (!_secp256K1.SignRecoverable(recSig, hash, privateKey))
                throw new SignatureOperationException("Create a recoverable ECDSA signature failed.");
            if (!_secp256K1.RecoverableSignatureSerializeCompact(compactSig, out var recoverId, recSig))
                throw new SignatureOperationException("Serialize an ECDSA signature failed.");
            compactSig[64] = (byte) recoverId; // put recover id at the last slot
            return compactSig;
        }

        public bool VerifySignature(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = RecoverPublicKey(signature, data, out var recoverPublicKey);
            return recoverResult && publicKey.BytesEqual(recoverPublicKey);
        }

        public bool RecoverPublicKey(byte[] signature, byte[] hash, out byte[] pubkey)
        {
            pubkey = null;
            // Recover id should be greater than or equal to 0 and less than 4
            if (signature.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH || signature.Last() >= 4)
                return false;
            var pubKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            var recoveredPubKey = new byte[Secp256k1.PUBKEY_LENGTH];
            var recSig = new byte[65];
            if (!_secp256K1.RecoverableSignatureParseCompact(recSig, signature, signature.Last()))
                return false;
            if (!_secp256K1.Recover(recoveredPubKey, recSig, hash))
                return false;
            if (!_secp256K1.PublicKeySerialize(pubKey, recoveredPubKey))
                return false;
            pubkey = pubKey;
            return true;
        }

        public byte[] EncryptMessage(byte[] senderPrivateKey, byte[] receiverPublicKey, byte[] plainText)
        {
            var ecdhKey = Ecdh(senderPrivateKey, receiverPublicKey);
            var newKeyPair = _crypto.GenerateKeys(KeyPairType.EC_SECP256K1, ecdhKey);
            return _crypto.Encrypt(plainText, newKeyPair.PublicKey);
        }

        public byte[] DecryptMessage(byte[] senderPublicKey, byte[] receiverPrivateKey, byte[] cipherText)
        {
            var ecdhKey = Ecdh(receiverPrivateKey, senderPublicKey);
            var newKeyPair = _crypto.GenerateKeys(KeyPairType.EC_SECP256K1, ecdhKey);
            return _crypto.Decrypt(cipherText, newKeyPair.PrivateKey);
        }

        public byte[] Ecdh(byte[] privateKey, byte[] publicKey)
        {
            var usablePublicKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            if (!_secp256K1.PublicKeyParse(usablePublicKey, publicKey))
                throw new PublicKeyOperationException("Parse public key failed.");
            var ecdhKey = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
            if (!_secp256K1.Ecdh(ecdhKey, usablePublicKey, privateKey))
                throw new EcdhOperationException("Compute EC Diffie- secret failed.");
            return ecdhKey;
        }

        public void Dispose()
        {
            _secp256K1?.Dispose();
        }
    }
}