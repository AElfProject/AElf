using System;
using System.Linq;
using Secp256k1Net;

namespace AElf.Cryptography.Core
{

    public sealed class Secp256k1Curve : IECCurve
    {
        private Secp256k1 _inner;

        public Secp256k1Curve()
        {
            _inner = new Secp256k1();
        }

        public IECPoint MultiplyScalar(IECPoint point, IECScalar scalar)
        {
            var output = point.Representation;
            if (!_inner.PublicKeyMultiply(output, scalar.Representation))
            {
                throw new FailedToMultiplyScalarException();
            }

            return Secp256k1Point.FromNative(output);
        }

        public IECPoint GetPoint(IECScalar scalar)
        {
            var pkBytes = new byte[Secp256k1.PUBKEY_LENGTH];
            if (!_inner.PublicKeyCreate(pkBytes, scalar.Representation))
            {
                throw new FailedToCreatePointFromScalarException();
            }

            return Secp256k1Point.FromNative(pkBytes);
        }

        public byte[] SerializePoint(IECPoint point, bool compressed)
        {
            var repr = point.Representation;
            if (compressed)
            {
                var serialized = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
                if (!_inner.PublicKeySerialize(serialized, repr, Flags.SECP256K1_EC_COMPRESSED))
                {
                    throw new FailedToSerializePointException();
                }

                return serialized;
            }
            else
            {
                var serialized = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                if (!_inner.PublicKeySerialize(serialized, repr, Flags.SECP256K1_EC_UNCOMPRESSED))
                {
                    throw new FailedToSerializePointException();
                }

                return serialized;
            }
        }


        public IECPoint Add(IECPoint point1, IECPoint point2)
        {
            var output = new byte[Secp256k1.PUBKEY_LENGTH];

            if (!_inner.PublicKeysCombine(output, point1.Representation, point2.Representation))
            {
                throw new FailedToCombinePublicKeysException();
            }

            return Secp256k1Point.FromNative(output);
        }

        public IECPoint Sub(IECPoint point1, IECPoint point2)
        {
            var point2Neg = point2.Representation;

            if (!_inner.PublicKeyNegate(point2Neg))
            {
                throw new FailedToNegatePublicKeyException();
            }

            return Add(point1, Secp256k1Point.FromNative(point2Neg));
        }

        public IECPoint DeserializePoint(byte[] input)
        {
            var pkBytes = new byte[Secp256k1.PUBKEY_LENGTH];
            if (!_inner.PublicKeyParse(pkBytes, input))
            {
                throw new InvalidSerializedPublicKeyException();
            }

            return Secp256k1Point.FromNative(pkBytes);
        }

        public IECScalar DeserializeScalar(byte[] input)
        {
            var normalized = Helpers.AddLeadingZeros(input, Secp256k1.PRIVKEY_LENGTH)
                .TakeLast(Secp256k1.PRIVKEY_LENGTH).ToArray();
            return Secp256k1Scalar.FromNative(normalized);
        }

        public byte[] GetNonce(IECScalar privateKey, byte[] hash)
        {
            var nonce = new byte[Secp256k1.NONCE_LENGTH];
            if (!_inner.Rfc6979Nonce(nonce, hash, privateKey.Representation, null, null, 0))
            {
                throw new FailedToGetNonceException();
            }

            return nonce;
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}