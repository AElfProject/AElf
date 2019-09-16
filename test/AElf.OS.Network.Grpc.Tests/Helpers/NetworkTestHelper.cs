using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;

namespace AElf.OS.Network
{
    public static class NetworkTestHelper
    {
        public static Handshake CreateValidHandshake(ECKeyPair producer, long bestChainHeight, int chainId = NetworkTestConstants.DefaultChainId, int port = 0)
        {
            var data = new HandshakeData
            {
                ChainId = chainId,
                Version = KernelConstants.ProtocolVersion,
                ListeningPort = port,
                Pubkey = ByteString.CopyFrom(producer.PublicKey),
                BestChainHash = Hash.FromString("BestChainHash"),
                BestChainHeight = bestChainHeight,
                LastIrreversibleBlockHash = Hash.FromString("LastIrreversibleBlockHash"),
                LastIrreversibleBlockHeight = 1,
                Time = TimestampHelper.GetUtcNow()
            };
            
            var signature = CryptoHelper.SignWithPrivateKey(producer.PrivateKey, Hash.FromMessage(data).ToByteArray());
            
            return new Handshake { HandshakeData = data, Signature = ByteString.CopyFrom(signature) };
        }

        public static BlockHeader CreateFakeBlockHeader(int chainId, long height, ECKeyPair producer)
        {
            return new BlockHeader
            {
                ChainId = chainId,
                Height = height,
                PreviousBlockHash = Hash.FromRawBytes(new byte[]{1, 2, 3}),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactions = Hash.Empty,
                MerkleTreeRootOfWorldState = Hash.Empty,
                MerkleTreeRootOfTransactionStatus = Hash.Empty,
                ExtraData = {ByteString.Empty},
                SignerPubkey = ByteString.CopyFrom(producer.PublicKey)
            };
        }
    }
}