using System;
using AElf.Cryptography.ECDSA;
using AElf.Network.Data;
using Google.Protobuf;

namespace AElf.Network.Tests
{
    public static class NetworkTestHelpers
    {
        public static (ECKeyPair, Handshake) CreateKeyPairAndHandshake(int port)
        {
            ECKeyPair key = new KeyPairGenerator().Generate();
            
            var nodeInfo = new NodeData { Port = port };
            
            ECSigner signer = new ECSigner();
            ECSignature sig = signer.Sign(key, nodeInfo.ToByteArray());
            
            var handshakeMsg = new Handshake
            {
                NodeInfo = nodeInfo,
                PublicKey = ByteString.CopyFrom(key.GetEncodedPublicKey()),
                R = ByteString.CopyFrom(sig.R),
                S = ByteString.CopyFrom(sig.S),
            };

            return (key, handshakeMsg);
        }

        public static byte[] GetRandomBytes(int size)
        {
            Random rnd = new Random();
            byte[] b = new byte[size];
            rnd.NextBytes(b);
            return b;
        }
    }
}