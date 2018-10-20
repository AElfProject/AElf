using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AElf.Common;
using AElf.Common.Extensions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace AElf.Cryptography.ECDSA
{
    // ReSharper disable once InconsistentNaming
    public class ECKeyPair
    {
//        public static int AddressLength { get; } = 18;

        public ECPrivateKeyParameters PrivateKey { get; private set; }
        public ECPublicKeyParameters PublicKey { get; private set; }

        public ECKeyPair(ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            PublicKey = publicKey;
            PrivateKey = privateKey;
        }

        public byte[] GetEncodedPublicKey(bool compressed = false)
        {
            return PublicKey.Q.GetEncoded(compressed);
        }

        public static ECKeyPair FromPublicKey(byte[] publicKey)
        {
            ECPublicKeyParameters pubKey
                = new ECPublicKeyParameters(ECParameters.Curve.Curve.DecodePoint(publicKey), ECParameters.DomainParams);

            ECKeyPair k = new ECKeyPair(null, pubKey);

            return k;
        }

        public Address GetAddress()
        {
            return Address.FromRawBytes(GetEncodedPublicKey());
        }

        public string GetAddressHex()
        {
            return Address.FromRawBytes(GetEncodedPublicKey()).Value.ToByteArray().ToHex();
            //"0x" + BitConverter.ToString(GetAddress()).Replace("-", string.Empty).ToLower();
        }
    }
}