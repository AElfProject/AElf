using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Common;
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

            var hash1 = blockHeader.GetHashWithoutCache();
            hash1.ShouldNotBeNull();
            hash1.ShouldBe(hash);
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
                TransactionList = { transactionItems.Item1},
                Transactions = { transactionItems.Item2},
                BinaryMerkleTree = {
                    Nodes = { Hash.Generate(), Hash.Generate() },
                    LeafCount = 2
                }
            };
            blockBody.TransactionsCount.ShouldBe(3);
            blockBody.BinaryMerkleTree.ShouldNotBe(null);
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
        public void BlockBody_Hash_Test()
        {
            var block = CreateBlock(Hash.Generate(), 0, 10);
            var hash = block.Body.GetHashWithoutCache();
            hash.ShouldNotBeNull();

            var hash1 = block.Body.GetHashWithoutCache();
            hash.ShouldNotBeNull();
            hash.ShouldBe(hash1);
        }
        
        [Fact]
        public void BlockTest()
        {
            var block = CreateBlock(Hash.Generate(), 1234, 10);
            block.Height.ShouldBe(10u);

            var hash = block.GetHash();
            hash.ShouldNotBe(null);

            var hash1 = block.GetHashWithoutCache();
            hash.ShouldBe(hash1);

            var hashBytes = block.GetHashBytes();
            hashBytes.ShouldNotBe(null);
            hashBytes.Length.ShouldBe(32);

            var serializeData = block.Serialize();
            serializeData.ShouldNotBe(null);
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
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.Height = height;
            block.Header.MerkleTreeRootOfWorldState = Hash.Empty;

            block.Body.BlockHeader = block.Header.GetHash();
            block.Body.BinaryMerkleTree.Root = Hash.Empty;
            var transactionItems = GenerateFakeTransactions(3);
            block.Body.TransactionList.AddRange(transactionItems.Item1);
            block.Body.Transactions.AddRange(transactionItems.Item2);

            return block;
        }

        private (List<Kernel.Transaction>, List<Hash>) GenerateFakeTransactions(int count)
        {
            var transactions = new List<Kernel.Transaction>();
            var transactionHashes = new List<Hash>();
            for (int i = 0; i < count; i++)
            {
               var transaction = new Kernel.Transaction()
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