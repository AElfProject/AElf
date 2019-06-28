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
            var blockHeader = new BlockHeader()
            {
                PreviousBlockHash = Hash.Generate(),
                ChainId = 1234,
                Height = 3,
            };
            var hash = blockHeader.GetHash();
            hash.ShouldNotBe(null);

            var hashByte = blockHeader.GetHashBytes();
            hashByte.ShouldNotBe(null);
        }

        [Fact]
        public void BlockBody_Test()
        {
            var blockHeader = new BlockHeader()
            {
                PreviousBlockHash = Hash.Generate(),
                ChainId = 1234,
                Height = 1,
            };
            var transactionItems = GenerateFakeTransactions(3);
            var blockBody = new BlockBody()
            {
                BlockHeader = blockHeader.GetHash(),
                Transactions = {transactionItems.Item2}
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
            var block = CreateBlock(Hash.Generate(), 1234, 10);
            block.Height.ShouldBe(10u);

            var hash = block.GetHash();
            hash.ShouldNotBe(null);
        }

        [Fact]
        public void Get_BlockHash()
        {
            var blockHeader = new BlockHeader
            {
                ChainId = 2111,
                Height = 10,
                PreviousBlockHash = Hash.Generate(),
                MerkleTreeRootOfTransactions = Hash.Generate(),
                MerkleTreeRootOfWorldState = Hash.Generate(),
                Bloom = ByteString.Empty,
                BlockExtraDatas = { ByteString.CopyFromUtf8("test")}
            };
            var hash = blockHeader.GetHash();
            hash.ShouldNotBeNull();   
            
            var hashBytes = blockHeader.GetHashBytes();
            hashBytes.Length.ShouldBe(32);

            var hash1 = Hash.LoadByteArray(hashBytes);
            hash.ShouldBe(hash1);
        }
        
        private Block CreateBlock(Hash preBlockHash, int chainId, long height)
        {
            Interlocked.CompareExchange(ref preBlockHash, Hash.Empty, null);

            var block = new Block(Hash.Generate());

            block.Header.PreviousBlockHash = preBlockHash;
            block.Header.ChainId = chainId;
            block.Header.Time = TimestampHelper.GetUtcNow();
            block.Header.Height = height;
            var transactionItems = GenerateFakeTransactions(3);
            block.Body.Transactions.AddRange(transactionItems.Item2);
            
            block.Header.MerkleTreeRootOfTransactions = block.Body.Transactions.ComputeBinaryMerkleTreeRootWithLeafNodes();
            block.Body.BlockHeader = block.Header.GetHash();           

            return block;
        }

        private (List<Transaction>, List<Hash>) GenerateFakeTransactions(int count)
        {
            var transactions = new List<Transaction>();
            var transactionHashes = new List<Hash>();
            for (int i = 0; i < count; i++)
            {
               var transaction = new Transaction()
               {
                   From = Address.Generate(),
                   To = Address.Generate(),
                   MethodName = $"Test{i}",
                   Params = ByteString.Empty
               };
               var hash = transaction.GetHash();

               transactions.Add(transaction);
               transactionHashes.Add(hash);
            }

            return (transactions, transactionHashes);
        }
    }
}