using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;

namespace AElf.OS.Helpers
{
    public static class OsCoreTestHelper
    {
        public static BlockHeader CreateFakeBlockHeader(int chainId, long height, ECKeyPair producer = null)
        {
            var signer = producer ?? CryptoHelper.GenerateKeyPair();

            return new BlockHeader
            {
                ChainId = chainId,
                Height = height,
                PreviousBlockHash = HashHelper.ComputeFrom(new byte[] {1, 2, 3} ),
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactions = Hash.Empty,
                MerkleTreeRootOfWorldState = Hash.Empty,
                MerkleTreeRootOfTransactionStatus = Hash.Empty,
                SignerPubkey = ByteString.CopyFrom(signer.PublicKey)
            };
        }

        public static Transaction CreateFakeTransaction()
        {
            var fromKeyPair = CryptoHelper.GenerateKeyPair();
            var toKeyPair = CryptoHelper.GenerateKeyPair();
            
            return new Transaction
            {
                From = Address.FromPublicKey(fromKeyPair.PublicKey),
                To = Address.FromPublicKey(toKeyPair.PublicKey),
                MethodName = "SomeMethod",
                RefBlockNumber = 2
            };
        }
    }
}