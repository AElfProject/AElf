using System.Collections.Generic;
using System.Threading;
using AElf.Types;
using Google.Protobuf;
using Xunit;
using Shouldly;

namespace AElf.Kernel.Types.Tests
{
    public class BlockTests
    {
        [Fact]
        public void BlockHeader_Test()
        {
            var blockHeader = GenerateBlockHeader();
            var hash = blockHeader.GetHash();
            hash.ShouldNotBe(null);

            var hashByte = blockHeader.GetHashBytes();
            hashByte.ShouldNotBe(null);
        }

        [Fact]
        public void BlockBody_Test()
        {
            var blockHeader = GenerateBlockHeader();
            var transactionItems = GenerateFakeTransactions(3);
            var blockBody = new BlockBody()
            {
                BlockHeader = blockHeader.GetHash(),
                TransactionIds = {transactionItems.Item2}
            };
            blockBody.TransactionsCount.ShouldBe(3);
        }

        [Fact]
        public void BlockIndex_Test()
        {
            var blockIndex = new BlockIndex();
            blockIndex.Hash = Hash.Empty;
            blockIndex.Height = 1L;
            
            var blockIndex1 = new BlockIndex(Hash.Empty, 1L);
            
            blockIndex.ToString().ShouldBe(blockIndex1.ToString());
            blockIndex.ToString().Contains(Hash.Empty.ToString()).ShouldBeTrue();
            blockIndex.ToString().Contains("1").ShouldBeTrue();
        }
        
        [Fact]
        public void BlockTest()
        {
            var block = CreateBlock(Hash.FromString("hash"), 1234, 10);
            block.Height.ShouldBe(10u);

            var hash = block.GetHash();
            hash.ShouldNotBe(null);
        }

        [Fact]
        public void Get_BlockHash()
        {
            var blockHeader = GenerateBlockHeader();
            var hash = blockHeader.GetHash();
            hash.ShouldNotBeNull();   
            
            var hashBytes = blockHeader.GetHashBytes();
            hashBytes.Length.ShouldBe(32);

            var hash1 = Hash.FromByteArray(hashBytes);
            hash.ShouldBe(hash1);
        }
        
        private Block CreateBlock(Hash preBlockHash, int chainId, long height)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Empty, null);

            var block = new Block(Hash.FromString("hash1"));

            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = TimestampHelper.GetUtcNow();
            block.Header.Height = height;
            var transactionItems = GenerateFakeTransactions(3);
            block.Body.TransactionIds.AddRange(transactionItems.Item2);
            
            block.Header.MerkleTreeRootOfTransactions = block.Body.TransactionIds.ComputeBinaryMerkleTreeRootWithLeafNodes();
            block.Header.MerkleTreeRootOfWorldState = Hash.Empty;
            block.Header.MerkleTreeRootOfTransactionStatus = Hash.Empty;
            block.Header.SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey");
            block.Header.ExtraData.Add(ByteString.Empty);
            block.Body.BlockHeader = block.Header.GetHash();           

            return block;
        }

        private (List<Transaction>, List<Hash>) GenerateFakeTransactions(int count)
        {
            var transactions = new List<Transaction>();
            var transactionIds = new List<Hash>();
            for (int i = 0; i < count; i++)
            {
               var transaction = new Transaction()
               {
                   From = AddressHelper.StringToAddress("from"),
                   To = AddressHelper.StringToAddress("to"),
                   MethodName = $"Test{i}",
                   Params = ByteString.Empty
               };
               var hash = transaction.GetHash();

               transactions.Add(transaction);
               transactionIds.Add(hash);
            }

            return (transactions, transactionIds);
        }

        private BlockHeader GenerateBlockHeader()
        {
            return new BlockHeader
            {
                ChainId = 1234,
                Height = 10,
                PreviousBlockHash = Hash.FromString("hash3"),
                MerkleTreeRootOfTransactions = Hash.Empty,
                MerkleTreeRootOfWorldState = Hash.Empty,
                ExtraData = { ByteString.Empty},
                Time = TimestampHelper.GetUtcNow(),
                MerkleTreeRootOfTransactionStatus = Hash.Empty,
                SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey")
            };
        }
    }
}