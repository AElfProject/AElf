using Org.BouncyCastle.Math;
using Secp256k1Net;

namespace AElf.Cryptography.ECVRF;

public struct Point
{
    private BigInteger P = new BigInteger("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", 16);
    public byte[] Inner { get; private set; }

    private Point(byte[] inner)
    {
        Inner = inner;
    }

    public static Point FromInner(byte[] inner)
    {
        return new Point() { Inner = inner };
    }

    public static Point FromSerialized(Secp256k1 secp256k1, byte[] pkSerialized)
    {
        if (pkSerialized.Length != Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH &&
            pkSerialized.Length != Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
        {
            throw new InvalidSerializedPublicKeyException();
        }

        if (pkSerialized.Length == Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH)
        {
            if (pkSerialized[0] != 0x02 && pkSerialized[0] != 0x03)
            {
                throw new InvalidSerializedPublicKeyException();
            }
        }

        if (pkSerialized.Length == Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH)
        {
            if (pkSerialized[0] != 0x04)
            {
                throw new InvalidSerializedPublicKeyException();
            }
        }

        var inner = new byte[Secp256k1.PUBKEY_LENGTH];
        var successful = secp256k1.PublicKeyParse(inner, pkSerialized);
        if (!successful)
        {
            throw new InvalidSerializedPublicKeyException();
        }

        return new Point() { Inner = inner };
    }

    public byte[] Serialize(Secp256k1 secp256k1, bool compressed = true)
    {
        if (compressed)
        {
            var output = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
            secp256k1.PublicKeySerialize(output, Inner, Flags.SECP256K1_EC_COMPRESSED);
            return output;
        }
        else
        {
            var output = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
            secp256k1.PublicKeySerialize(output, Inner, Flags.SECP256K1_EC_UNCOMPRESSED);
            return output;
        }
    }
}