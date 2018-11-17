using AElf.Cryptography.ECDSA;
using AElf.Common;
using Org.BouncyCastle.Crypto.Tls;

namespace AElf.Node
{
    public class NodeKeyPair : ECKeyPair
    {
        private Address _address;
        private byte[] _compressedEncodedPublicKey;
        private byte[] _nonCompressedEncodedPublicKey;
        
        public NodeKeyPair(ECKeyPair keyPair) : base(keyPair.PrivateKey, keyPair.PublicKey)
        {
        }

        public Address Address {
            get
            {
                if (_address == null)
                {
                    _address = GetAddress();
                }

                return _address;
            }
        }

        public byte[] CompressedEncodedPublicKey
        {
            get
            {
                if (_compressedEncodedPublicKey == null)
                {
                    _compressedEncodedPublicKey = GetEncodedPublicKey(true);
                }

                return _compressedEncodedPublicKey;
            }
        }

        public byte[] NonCompressedEncodedPublicKey
        {
            get
            {
                if (_nonCompressedEncodedPublicKey == null)
                {
                    _nonCompressedEncodedPublicKey = GetEncodedPublicKey(false);
                }

                return _nonCompressedEncodedPublicKey;
            }
        }
    }
}