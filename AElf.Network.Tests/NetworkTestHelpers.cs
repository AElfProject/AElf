using System;
using System.Security.Cryptography;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Network.Data;
using Google.Protobuf;

namespace AElf.Network.Tests
{
    public static class NetworkTestHelpers
    {
        public static (ECKeyPair, Handshake) CreateKeyPairAndHandshake(int port)
        {
            ECKeyPair key = CryptoHelpers.GenerateKeyPair();
            
            var nodeInfo = new NodeData { Port = port };
            
            var sig = CryptoHelpers.SignWithPrivateKey(key.PrivateKey, SHA256.Create().ComputeHash(nodeInfo.ToByteArray()));
            
            var handshakeMsg = new Handshake
            {
                NodeInfo = nodeInfo,
                PublicKey = ByteString.CopyFrom(key.PublicKey),
                Sig = ByteString.CopyFrom(sig),
                Version = GlobalConfig.ProtocolVersion,
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